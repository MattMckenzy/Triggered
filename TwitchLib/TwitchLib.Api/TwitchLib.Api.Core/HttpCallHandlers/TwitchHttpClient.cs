using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.Internal;

namespace TwitchLib.Api.Core.HttpCallHandlers
{
    public class TwitchHttpClient : IHttpCallHandler
    {
        private readonly ILogger<TwitchHttpClient> _logger;
        private readonly HttpClient _http;
        private readonly Func<Task<string>> _tokenRefreshDelegate;
        private readonly Func<Exception, Task> _webExceptionHandler;

        /// <summary>
        /// Creates an Instance of the TwitchHttpClient Class.
        /// </summary>
        /// <param name="logger">Instance Of Logger, otherwise no logging is used,  </param>
        public TwitchHttpClient(ILogger<TwitchHttpClient> logger = null, Func<Task<string>> tokenRefreshDelegate = null, Func<Exception, Task> webExceptionHandler = null)
        {
            _logger = logger;
            _http = new HttpClient(new TwitchHttpClientHandler(_logger));
            _tokenRefreshDelegate = tokenRefreshDelegate;
            _webExceptionHandler = webExceptionHandler;
        }


        public async Task PutBytes(string url, byte[] payload)
        {
            var response = _http.PutAsync(new Uri(url), new ByteArrayContent(payload)).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
                await HandleWebException(response);
        }

        public async Task<KeyValuePair<int, string>> GeneralRequest(string url, string method, string payload = null, ApiVersion api = ApiVersion.V5, string clientId = null, string accessToken = null, bool refreshedToken = false)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = new HttpMethod(method)
            };

            if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(accessToken))
                throw new InvalidCredentialException("A Client-Id or OAuth token is required to use the Twitch API. If you previously set them in InitializeAsync, please be sure to await the method.");

            if (!string.IsNullOrEmpty(clientId))
            {
                request.Headers.Add("Client-ID", clientId);
            }

            request.Headers.Add("Cache-Control", "no-cache  ");
            
            var authPrefix = "OAuth";
            if (api == ApiVersion.Helix)
            {
                request.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");
                authPrefix = "Bearer";
            }
            else if (api != ApiVersion.Void)
            {
                request.Headers.Add(HttpRequestHeader.Accept.ToString(), $"application/vnd.twitchtv.v{(int)api}+json");
            }
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Add(HttpRequestHeader.Authorization.ToString(), $"{authPrefix} {Common.Helpers.FormatOAuth(accessToken)}");

            if (payload != null)
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var response = _http.SendAsync(request).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var respStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return new KeyValuePair<int, string>((int)response.StatusCode, respStr);
                }

                await HandleWebException(response);
            }
            catch (UnauthorizedException exception)
            {
                if (refreshedToken == false && _tokenRefreshDelegate != null)
                {
                    string refreshedAccessToken = await _tokenRefreshDelegate();
                    return await GeneralRequest(url, method, payload, api, clientId, refreshedAccessToken, true);
                }
                else
                    throw exception;
            }

            return new KeyValuePair<int, string>(0, null);
        }

        public int RequestReturnResponseCode(string url, string method, List<KeyValuePair<string, string>> getParams = null)
        {
            if (getParams != null)
            {
                for (var i = 0; i < getParams.Count; i++)
                {
                    if (i == 0)
                        url += $"?{getParams[i].Key}={Uri.EscapeDataString(getParams[i].Value)}";
                    else
                        url += $"&{getParams[i].Key}={Uri.EscapeDataString(getParams[i].Value)}";
                }
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = new HttpMethod(method)
            };
            var response = _http.SendAsync(request).GetAwaiter().GetResult();
            return (int)response.StatusCode;
        }

        private async Task HandleWebException(HttpResponseMessage errorResp)
        {
            try
            {           
                string content = errorResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                switch (errorResp.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new BadRequestException($"The request was returned as bad{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedException($"You are unauthorized for this request{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                    case HttpStatusCode.NotFound:
                        throw new BadResourceException($"The resource was not found{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                    case (HttpStatusCode)422:
                        throw new NotPartneredException("The resource you requested is only available to channels that have been partnered by Twitch.");
                    case (HttpStatusCode)429:
                        errorResp.Headers.TryGetValues("Ratelimit-Reset", out var resetTime);
                        throw new TooManyRequestsException("You have reached your rate limit. Too many requests were made", resetTime.FirstOrDefault());
                    case HttpStatusCode.BadGateway:
                        throw new BadGatewayException("The API answered with a 502 Bad Gateway. Please retry your request");
                    case HttpStatusCode.GatewayTimeout:
                        throw new GatewayTimeoutException("The API answered with a 504 Gateway Timeout. Please retry your request");
                    case HttpStatusCode.InternalServerError:
                        throw new InternalServerErrorException("The API answered with a 500 Internal Server Error. Please retry your request");
                    case HttpStatusCode.Forbidden:
                        throw new UnauthorizedException("The token provided in the request did not match the associated user. Make sure the token you're using is from the resource owner (streamer? viewer?)");
                    default:
                        throw new HttpRequestException("Something went wrong during the request! Please try again later");
                }
            }
            catch (WebException webException)
            {
                if (_webExceptionHandler != null)
                    await _webExceptionHandler(webException);
                else
                    throw webException;
            }
        }

    }
}