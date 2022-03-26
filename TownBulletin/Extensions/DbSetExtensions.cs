using Microsoft.EntityFrameworkCore;
using TownBulletin.Models;

namespace TownBulletin.Extensions
{
    public static class DbSetExtensions
    {
        private static readonly Dictionary<string, string> defaultSettings = new()
        {
            { "Host", "https://localhost" },
            { "WebhookHost", "https://{external_ip}" },
            { "Autostart", "true" },
            { "MessageLevels", "Information, Warning, Error" },
            { "MessageNotificationsEnabled", "False" },
            { "MessageNotificationVolume", "0.25" },
            { "ObsAddress", "wss://localhost:444" },
            { "ObsPassword", "" },
            { "TwitchClientId", "" },
            { "TwitchClientSecret", "" },
            { "TwitchUserName", "" },
            { "TwitchChannelName", "" },
            { "TwitchAccessToken", "" },
            { "TwitchRefreshToken", "" },
            { "TwitchExtensionSubscriptoins", "" },
            { "TwitchDropSubscriptoins", "" },
            { "TwitchBotClientId", "" },
            { "TwitchBotClientSecret", "" },
            { "TwitchBotUserName", "" },
            { "TwitchBotChannelName", "" },
            { "TwitchBotAccessToken", "" },
            { "TwitchBotRefreshToken", "" }
        };

        public static string GetSetting(this DbSet<Setting> dbSet, string name)
        {
            Setting? setting = dbSet.FirstOrDefault(s => s.Key == name);

            if (setting == null)
            {
                defaultSettings.TryGetValue(name, out string? value);
                dbSet.SetSetting(name, value ?? string.Empty);
                return value ?? string.Empty;
            }
            else
                return setting.Value;
        }

        public static void SetSetting(this DbSet<Setting> dbSet, string name, string value)
        {
            Setting? setting = dbSet.FirstOrDefault(setting => setting.Key.Equals(name));
            if (setting == null)
            {
                dbSet.Add(new Setting(name, value)).Context.SaveChanges();
            }
            else
            {
                setting.Value = value;
                dbSet.Update(setting).Context.SaveChanges();
            }
        }

        public static void Populate(this DbSet<Setting> dbSet)
        {
            foreach (string key in defaultSettings.Keys)
                dbSet.GetSetting(key);
        }      
    }
}