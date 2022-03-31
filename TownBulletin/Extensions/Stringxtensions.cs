using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TownBulletin.Extensions
{
    public static class StringExtensions
    {
        public static bool TryCreateDirectory(this string path, out DirectoryInfo? directoryInfo)
        {
            try
            {
                directoryInfo = Directory.CreateDirectory(path);
                return true;
            }
            catch { }

            directoryInfo = null;
            return false;
        }
    }
}
