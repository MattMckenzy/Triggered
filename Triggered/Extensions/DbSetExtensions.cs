using Microsoft.EntityFrameworkCore;
using Triggered.Models;

namespace Triggered.Extensions
{
    public static class DbSetExtensions
    {
        private static readonly Dictionary<string, string> defaultSettings = new()
        {
            { "Host", "https://localhost" },
            { "WebhookHost", "https://{external_ip}" },
            { "Autostart", "true" },
            { "ModuleTemplate", 
              @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace Triggered.Modules./*EventName*/
{
    public class /*ModuleName*/
    {
        public static Task<bool> /*EntryMethod*/(/*EventArgs*/ eventArgs)
        {
            return Task.FromResult(true);
        }
    }
}" 
            },
            { "ExternalModulesPath", "Modules" },
            { "ExternalResourcesPath", "Resources" },
            { "MessagesLimit", "1000" },
            { "MessageLevels", "Information, Warning, Error" },
            { "MessageNotificationsEnabled", "False" },
            { "MessageNotificationVolume", "0.25" },
            { "ObsAddress", "ws://localhost:4444" },
            { "ObsPassword", "" },
            { "TwitchClientId", "" },
            { "TwitchClientSecret", "" },
            { "TwitchUserName", "" },
            { "TwitchChannelName", "" },
            { "TwitchAccessToken", "" },
            { "TwitchRefreshToken", "" },
            { "TwitchExtensionSubscriptoins", "" },
            { "TwitchDropSubscriptoins", "" },
            { "TwitchChatUseSecondAccount", "False" },
            { "TwitchChatClientId", "" },
            { "TwitchChatClientSecret", "" },
            { "TwitchChatUserName", "" },
            { "TwitchChatChannelName", "" },
            { "TwitchChatAccessToken", "" },
            { "TwitchChatRefreshToken", "" },
            { "DiscordBotToken", "" }
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