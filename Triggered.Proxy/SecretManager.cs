using System.Collections.Concurrent;
using Triggered.Extensions;

namespace Triggered.Proxy
{
    public class SecretManager
    {
        public ConcurrentDictionary<string, List<string>> Secrets { get; } = new();

        public Task<string> GetSecret(int secretSize, string connectionId)
        {
            string secret;
            do
            {
                secret = secretSize.GetThisRandomStringLength();
            } while (!Secrets.TryAdd(secret, new() { }));
            return Task.FromResult(secret);
        }
    }
}
