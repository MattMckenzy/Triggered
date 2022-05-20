using Microsoft.Extensions.Logging;
using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ModuleMaker.Utilities
{
    public static class TwitchBadgeUtilities
    {
        /// <summary>
        /// Retrieves from cache or from Twitch a dynamic <see cref="ExpandoObject"/> containing information on a Twitch badge.
        /// </summary>
        /// <param name="dataService">An instance of <see cref="DataService"/> that is used to cache the retrieved Twitch user.</param>
        /// <param name="twitchService">An instance of <see cref="TwitchService"/> that is used to retrieve a user.</param>
        /// <param name="messagingService">An instance of <see cref="MessagingService"/> that is used to warn of issues with the Twitch API communication.</param>
        /// <param name="badgeKey">The key of the badge to retrieve.</param>
        /// <returns>An <see cref="ExpandoObject"/> that contains the following properties: Expires, Key, ImageUrl1x, ImageUrl2x, ImageUrl4x.</returns>
        public static async Task<ExpandoObject?> GetTwitchBadge(DataService dataService, TwitchService twitchService, MessagingService messagingService, string badgeKey)
        {
            ExpandoObject? badge = await dataService.GetObject($"Twitch.Badges.{badgeKey}");
            if (badge == null || ((dynamic)badge).Expires < DateTime.Now)
            {
                try
                {
                    GetChannelChatBadgesResponse channelBadgesResponse = await twitchService.TwitchAPI.Helix.Chat.GetChannelChatBadgesAsync(twitchService.User?.Id);
                    foreach (BadgeEmoteSet badgeEmoteSet in channelBadgesResponse.EmoteSet)
                    {
                        foreach (BadgeVersion badgeVersion in badgeEmoteSet.Versions)
                        {
                            badge = new ExpandoObject();
                            string key = $"{badgeEmoteSet.SetId}.{badgeVersion.Id}";
                            ((dynamic)badge).Expires = DateTime.Now + TimeSpan.FromDays(7);
                            ((dynamic)badge).Key = key;
                            ((dynamic)badge).ImageUrl1x = badgeVersion.ImageUrl1x;
                            ((dynamic)badge).ImageUrl2x = badgeVersion.ImageUrl2x;
                            ((dynamic)badge).ImageUrl4x = badgeVersion.ImageUrl4x;
                            await dataService.SetObject($"Twitch.Badges.{key}", badge);
                        }
                    }
                }
                catch (Exception exception)
                {
                    await messagingService.AddMessage($"Error getting channel badges: {exception.Message}", MessageCategory.Module, LogLevel.Error);
                }

                try
                {
                    GetGlobalChatBadgesResponse globalBadgesResponse = await twitchService.TwitchAPI.Helix.Chat.GetGlobalChatBadgesAsync();
                    foreach (BadgeEmoteSet badgeEmoteSet in globalBadgesResponse.EmoteSet)
                    {
                        foreach(BadgeVersion badgeVersion in badgeEmoteSet.Versions)
                        {
                            badge = new ExpandoObject();
                            string key = $"{badgeEmoteSet.SetId}.{badgeVersion.Id}";
                            ((dynamic)badge).Expires = DateTime.Now + TimeSpan.FromDays(7);
                            ((dynamic)badge).Key = key;
                            ((dynamic)badge).ImageUrl1x = badgeVersion.ImageUrl1x;
                            ((dynamic)badge).ImageUrl2x = badgeVersion.ImageUrl2x;
                            ((dynamic)badge).ImageUrl4x = badgeVersion.ImageUrl4x;
                            await dataService.SetObject($"Twitch.Badges.{key}", badge);
                        }
                    }
                }
                catch (Exception exception)
                {
                    await messagingService.AddMessage($"Error getting global badges: {exception.Message}", MessageCategory.Module, LogLevel.Error);
                }

                badge = await dataService.GetObject($"Twitch.Badges.{badgeKey}");

                if (badge == null)
                {
                    await messagingService.AddMessage("Could not retrieve badge from Twitch.", MessageCategory.Module, LogLevel.Error);
                    return null;
                }
            }

            return badge;
        }
    }
}