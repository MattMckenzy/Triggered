using Microsoft.EntityFrameworkCore;
using Triggered.Models;

namespace Triggered.Extensions
{
    /// <summary>
    /// A set of <see cref="DbSet{TEntity}"/> extensions for <see cref="Setting"/> to quickly retrieve or save. Manages default values for pre-defined settings.
    /// </summary>
    public static class DbSetExtensions
    {
        private static readonly Dictionary<string, string> defaultSettings = new()
        {
            { "Host", "https://localhost:7121" },
            { "WebhookHost", "https://proxy.triggered.events" },
            { "UseWebhookHostProxy", "True" },
            { "Autostart", "true" },
            { "ModuleTemplate", 
              @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;

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
            {
                "UtilityTemplate",
                @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Utilities
{
    public class UtilitiesClass
    {
        public static Task UtilitiesMethod()
        {
            return Task.CompletedTask;
        }
    }
}"
            },
            { "ExternalModulesPath", "Modules" },
            { "ExternalUtilitiesPath", "Utilities" },
            { "ExternalResourcesPath", "Resources" },
            { "FileWatcherPaths", "" },
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

        /// <summary>
        /// Retrieves a DB <see cref="Setting"/> with the given <paramref name="name"/>. If not found, returns the default value or an empty string if no default value defined.
        /// </summary>
        /// <param name="dbSet">From extension, the settings <see cref="DbSet{Setting}"/>.</param>
        /// <param name="name">The name of the setting to retrieve.</param>
        /// <returns>The value of the setting, or a default or empty string if not found.</returns>
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

        /// <summary>
        /// Saves a DB <see cref="Setting"/> with the given <paramref name="name"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="dbSet">From extension, the settings <see cref="DbSet{Setting}"/>.</param>
        /// <param name="name">The name of the setting to create or update.</param>
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

        /// <summary>
        /// Populates the DB with the default string value for any default settings that are missing.
        /// </summary>
        /// <param name="dbSet">From extension, the settings <see cref="DbSet{Setting}"/>.</param>
        public static void Populate(this DbSet<Setting> dbSet)
        {
            foreach (string key in defaultSettings.Keys)
                dbSet.GetSetting(key);
        }      
    }
}