using System.Security.Cryptography;
using System.Text;

namespace TownBulletin.Extensions
{
    public static class IntExtensions
    {
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
