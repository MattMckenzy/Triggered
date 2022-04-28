namespace Triggered.Extensions
{
    /// <summary>
    /// A string extension create a directory from the given path.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Tries to create a directory with the given <paramref name="path"/> <see cref="string"/>.
        /// </summary>
        /// <param name="path">The <see cref="string"/> <paramref name="path"/> that will be used to create the directory.</param>
        /// <param name="directoryInfo">Optional out parameter, a <see cref="DirectoryInfo"/> referecing the created directory, if it was succesful, null otherwise.</param>
        /// <returns>True if creating the directory was succesful, false otherwise.</returns>
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
