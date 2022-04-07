using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Triggered.Proxy
{
    public class ProxyHub : Hub
    {
        private SecretManager SecretManager { get; }

        public ProxyHub(SecretManager secretManager)
        {
            SecretManager = secretManager;
        }

        public async Task<string> GetSecret(int secretSize)
        {
            return await SecretManager.GetSecret(secretSize, Context.ConnectionId);
        }

        public override Task OnConnectedAsync()
        {
            string? secret = Context.GetHttpContext()?.Request.Query["secret"];
            if (secret != null && SecretManager.Secrets.TryGetValue(secret, out List<string>? connectionIds) && connectionIds != null)
            {
                lock(connectionIds)
                {
                    connectionIds.Add(Context.ConnectionId);
                }
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string? secret = Context.GetHttpContext()?.Request.Query["secret"];
            if (secret != null && SecretManager.Secrets.TryGetValue(secret, out List<string>? connectionIds) && connectionIds != null)
            {
                lock (connectionIds)
                {
                    connectionIds.Remove(Context.ConnectionId);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}