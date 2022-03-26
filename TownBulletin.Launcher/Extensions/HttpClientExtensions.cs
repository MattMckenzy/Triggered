using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TownBulletin.Launcher.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<(long, long)>? progress = null, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            long? contentLength = response.Content.Headers.ContentLength;

            using Stream download = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (progress == null || !contentLength.HasValue)
            {
                await download.CopyToAsync(destination, cancellationToken);
                return;
            }

            Progress<long> relativeProgress = new(totalBytes => progress.Report((totalBytes, contentLength.Value)));
            // Use extension method to report progress while downloading
            await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
            progress.Report((contentLength.Value, contentLength.Value));
        }
    }
}
