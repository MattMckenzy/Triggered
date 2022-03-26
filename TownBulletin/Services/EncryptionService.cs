using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TownBulletin.Models;

namespace TownBulletin.Services
{
    public class EncryptionService
    {
        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;
        private readonly IConfiguration _configuration;
        private readonly MessagingService _messagingService;

        public EncryptionService(IDbContextFactory<TownBulletinDbContext> dbContextFactory, IConfiguration configuration, MessagingService messagingService)
        {
            _dbContextFactory = dbContextFactory;
            _configuration = configuration;
            _messagingService = messagingService;
        }

        public async Task<string> Encrypt(string key, string value)
        {
            using Aes aes = Aes.Create();
            aes.Key = GetKey(_configuration["EncryptionKey"]);
            aes.Padding = PaddingMode.PKCS7;

            await SaveVector(key, Convert.ToBase64String(aes.IV));
            try
            {
                using MemoryStream encryptionStream = new();
                using CryptoStream cryptoStream = new(encryptionStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                using (StreamWriter encryptWriter = new(cryptoStream))
                {
                    encryptWriter.Write(value);
                }
                return Convert.ToBase64String(encryptionStream.ToArray());

            }
            catch (Exception _) when (_ is FormatException || _ is CryptographicException)
            {
                await _messagingService.AddMessage($"Could not encrypt {key}.", MessageCategory.Authentication, LogLevel.Error);
                return string.Empty;
            }
        }

        public async Task<string> Decrypt(string key, string value)
        {
            using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

            using Aes aes = Aes.Create();
            aes.Key = GetKey(_configuration["EncryptionKey"]);
            aes.Padding = PaddingMode.PKCS7;

            string? IV = townBulletinDbContext.Vectors.FirstOrDefault(vector => vector.Key == key)?.Value;
            if (string.IsNullOrWhiteSpace(IV))
                return string.Empty;

            aes.IV = Convert.FromBase64String(IV);
            try
            {
                using MemoryStream decryptionStream = new(Convert.FromBase64String(value));
                using CryptoStream cryptoStream = new(decryptionStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader decryptReader = new(cryptoStream);
                return decryptReader.ReadToEnd();
            }
            catch (Exception _) when (_ is FormatException || _ is CryptographicException)
            {
                await _messagingService.AddMessage($"Could not decrypt {key}.", MessageCategory.Authentication, LogLevel.Error);
                return string.Empty;
            }
        }

        public async Task SaveVector(string key, string value)
        {
            using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

            Vector? vector = townBulletinDbContext.Vectors.FirstOrDefault(setting => setting.Key.Equals(key));
            if (vector == null)
            {
                townBulletinDbContext.Vectors.Add(new Vector(key, value)).Context.SaveChanges();
            }
            else
            {
                vector.Value = value;
                townBulletinDbContext.Vectors.Update(vector);
            }

            await townBulletinDbContext.SaveChangesAsync();
        }

        private static byte[] GetKey(string passcode, int keyBytes = 32)
        {
            Rfc2898DeriveBytes keyGenerator = new(passcode, Salt, Iterations);
            return keyGenerator.GetBytes(keyBytes);        
        }    

        private static readonly byte[] Salt = new byte[] { 45, 07, 10, 55, 72, 60, 32, 77 };
        private static readonly int Iterations = 121;
    }
}
