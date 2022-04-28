using System.Security.Cryptography;
using System.Text;

namespace Triggered.Extensions
{
    /// <summary>
    /// An int extension to help retrieve a random alphanumeric string of the given <see cref="int"/> length.
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// Retrieves a random alphanumeric string of the given <see cref="int"/> length. The string can also contain the characters "?" and "!".
        /// </summary>
        /// <param name="length">The length of the string to retrieve.</param>
        /// <returns>The random string.</returns>
        public static string GetThisRandomStringLength(this int length)
        {
            const string validCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890?!";
            StringBuilder resultString = new();
            using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
            
            byte[] uintBuffer = new byte[sizeof(uint)];

            while (length-- > 0)
            {
                randomNumberGenerator.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                resultString.Append(validCharacters[(int)(num % (uint)validCharacters.Length)]);
            }            

            return resultString.ToString();
        }
    }
}
