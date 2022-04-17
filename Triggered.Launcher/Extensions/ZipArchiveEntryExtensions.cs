using System.IO;
using System.IO.Compression;

namespace Triggered.Launcher.Extensions
{
    public static class ZipArchiveEntryExtensions
    {
        public static byte[] GetBytes(this ZipArchiveEntry zipArchiveEntry)
        {
            byte[] returnBytes;

            using Stream stream = zipArchiveEntry.Open();
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            returnBytes = memoryStream.ToArray();

            return returnBytes;
        }
    }
}
