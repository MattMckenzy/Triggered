using System;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.EventSub.Webhooks.Core;
using Microsoft.AspNetCore.SignalR;

namespace Triggered.Proxy.EventSub.Webhooks.Middlewares
{
    public class EventSubNotificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITwitchEventSubWebhooks _eventSubWebhooks;
        private readonly IHubContext<ProxyHub> _proxyHubContext;

        public SecretManager SecretManager { get; }

        public EventSubNotificationMiddleware(RequestDelegate next, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<ProxyHub> proxyHubContext, SecretManager secretManager)
        {
            _next = next;
            _eventSubWebhooks = eventSubWebhooks;
            _proxyHubContext = proxyHubContext;
            SecretManager = secretManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Twitch-Eventsub-Message-Type", out var messageType))
            {
                await _next(context);
                return;
            }

            var headers = context.Request.Headers.ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            switch (messageType)
            {
                case "webhook_callback_verification":  
                    var json = await JsonDocument.ParseAsync(context.Request.Body);
                    await WriteResponseAsync(context, 200, "text/plain", json.RootElement.GetProperty("challenge").GetString()!);
                    return;
                case "notification":
                    if (context.Items.TryGetValue("connectionId", out object? secret) && 
                        secret != null && 
                        secret is string secretString &&
                        SecretManager.Secrets.TryGetValue(secretString, out List<string>? connectionIds) &&
                        connectionIds != null &&
                        connectionIds.Any())
                    {
                        using StreamReader streamReader = new(context.Request.Body);
                        await _proxyHubContext.Clients.Clients(connectionIds).SendAsync("ProcessEventNotification", headers, await streamReader.ReadToEndAsync());
                    }
                    else
                        await _eventSubWebhooks.ProcessNotificationAsync(headers, context.Request.Body);
                    await WriteResponseAsync(context, 200, "text/plain", "Thanks for the heads up Jordan");
                    return;
                case "revocation":
                    await _eventSubWebhooks.ProcessRevocationAsync(headers, context.Request.Body);
                    await WriteResponseAsync(context, 200, "text/plain", "Thanks for the heads up Jordan");
                    return;
                default:
                    await WriteResponseAsync(context, 400, "text/plain", $"Unknown EventSub message type: {messageType}");
                    return;
            }
        }

        private static async Task WriteResponseAsync(HttpContext context, int statusCode, string contentType, string responseBody)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            await context.Response.WriteAsync(responseBody);
        }
    }
}