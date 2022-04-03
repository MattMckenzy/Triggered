using System.Net;

namespace Triggered.Middleware
{

    public class IPAccessListMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IPAccessListMiddleware> _logger;
        private readonly byte[][] _safelist;

        public IPAccessListMiddleware(RequestDelegate next, ILogger<IPAccessListMiddleware> logger, string safelist)
        {
            string[] ips = safelist.Split(';');
            _safelist = new byte[ips.Length][];
            for (int i = 0; i < ips.Length; i++)
            {
                _safelist[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
            }

            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            IPAddress? remoteIp = context.Connection.RemoteIpAddress;
            _logger.LogDebug("Request from IP address: {RemoteIp}", remoteIp);

            byte[] bytes = remoteIp?.GetAddressBytes() ?? Array.Empty<byte>();
            bool badIp = true;
            foreach (byte[] address in _safelist)
            {
                if (address.SequenceEqual(bytes))
                {
                    badIp = false;
                    break;
                }
            }

            if (badIp)
            {
                _logger.LogWarning(
                    "Forbidden Request from Remote IP address: {RemoteIp}", remoteIp);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            await _next.Invoke(context);
        }
    }
}