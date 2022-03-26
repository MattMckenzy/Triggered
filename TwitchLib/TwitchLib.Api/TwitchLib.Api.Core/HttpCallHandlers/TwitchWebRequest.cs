using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.Interfaces;

namespace TwitchLib.Api.Core.HttpCallHandlers
{
    public class TwitchWebRequest : IHttpCallHandler
    {
        private readonly ILogger<TwitchWebRequest> _logger;

        /// <summary>
        /// Creates an Instance of the TwitchHttpClient Class.
        /// </summary>
        /// <param name="logger">Instance Of Logger, otherwise no logging is used,  </param>
        public TwitchWebRequest(ILogger<TwitchWebRequest> logger = null)
        {
            _logger = logger;
        }


        public void PutBytes(string url, byte[] payload)
        {
            try
            {
                using (var client = new WebClient())
                    client.UploadData(new Uri(url), "PUT", payload);
            }
            catch (WebException ex) { HandleWebException(ex); }
        }

        public KeyValuePair<int, string> GeneralRequest(string url, string method, string payload = null, ApiVersion api = ApiVersion.V5, string clientId = null, string accessToken = null)
        {
            var request = WebRequest.CreateHttp(url);
            if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(accessToken))
                throw new InvalidCredentialException("A Client-Id or OAuth token is required to use the Twitch API. If you previously set them in InitializeAsync, please be sure to await the method.");


            if (!string.IsNullOrEmpty(clientId))
            {
                request.Headers["Client-ID"] = clientId;
            }
           

            request.Method = method;
            request.ContentType = "application/json";

            var authPrefix = "OAuth";
            if (api == ApiVersion.Helix)
            {
                request.Accept = "application/json";
                authPrefix = "Bearer";
            }
            else if (api != ApiVersion.Void)
            {
                request.Accept = $"application/vnd.twitchtv.v{(int)api}+json";
            }

            if (!string.IsNullOrEmpty(accessToken))
                request.Headers["Authorization"] = $"{authPrefix} {Common.Helpers.FormatOAuth(accessToken)}";
            

            if (payload != null)
                using (var writer = new StreamWriter(request.GetRequestStreamAsync().GetAwaiter().GetResult()))
                    writer.Write(payload);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()))
                {
                    var data = reader.ReadToEnd();
                    return new KeyValuePair<int, string>((int)response.StatusCode, data);
                }
            }
            catch (WebException ex) { HandleWebException(ex); }

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

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            var response = (HttpWebResponse)req.GetResponse();
            return (int)response.StatusCode;
        }

        private void HandleWebException(WebException e)
        {
            if (!(e.Response is HttpWebResponse errorResp))
                throw e;

            string content = string.Empty;
            Encoding encoding = Encoding.GetEncoding(errorResp.CharacterSet);
            using (Stream responseStream = errorResp.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, encoding))
                content = reader.ReadToEnd();

            switch (errorResp.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException($"The request was returned as bad{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedException($"You are unauthorized for this request{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                case HttpStatusCode.NotFound:
                    throw new BadResourceException($"The resource was not found{(string.IsNullOrWhiteSpace(content) ? "." : $" with the following message: {content}")}");
                case (HttpStatusCode)429:
                    var resetTime = errorResp.Headers.Get("Ratelimit-Reset");
                    throw new TooManyRequestsException("You have reached your rate limit. Too many requests were made", resetTime);
                case (HttpStatusCode)422:
                    throw new NotPartneredException("The resource you requested is only available to channels that have been partnered by Twitch.");
                default:
                    throw e;
            }
        }

    }
}
