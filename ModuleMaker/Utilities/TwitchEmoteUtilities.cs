using Microsoft.Extensions.Logging;
using System;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using TwitchLib.Api.Helix.Models.Chat.Emotes.GetChannelEmotes;
using TwitchLib.Api.Helix.Models.Chat.Emotes.GetGlobalEmotes;
using TwitchLib.Client.Models;

namespace ModuleMaker.Utilities
{
    public static class TwitchEmoteUtilities
    {
        /// <summary>
        /// Retrieves from cache or from Twitch a dynamic <see cref="ExpandoObject"/> containing information on a Twitch emote.
        /// </summary>
        /// <param name="dataService">An instance of <see cref="DataService"/> that is used to cache the retrieved Twitch user.</param>
        /// <param name="twitchService">An instance of <see cref="TwitchService"/> that is used to retrieve a user.</param>
        /// <param name="messagingService">An instance of <see cref="MessagingService"/> that is used to warn of issues with the Twitch API communication.</param>
        /// <param name="badgeKey">The key of the badge to retrieve.</param>
        /// <returns>An <see cref="ExpandoObject"/> that contains the following properties: Expires, Id, Name, Tier, ImageUrl1x, ImageUrl2x, ImageUrl4x.</returns>
        public static async Task<ExpandoObject?> GetTwitchEmote(DataService dataService, TwitchService twitchService, MessagingService messagingService, string emoteKey)
        {
            ExpandoObject? emote = await dataService.GetObject($"Twitch.Emotes.{emoteKey}");
            if (emote == null || ((dynamic)emote).Expires < DateTime.Now)
            {
                try
                {
                    GetChannelEmotesResponse channelEmotesResponse = await twitchService.TwitchAPI.Helix.Chat.GetChannelEmotesAsync(twitchService.User?.Id);
                    foreach (ChannelEmote channelEmote in channelEmotesResponse.ChannelEmotes)
                    {
                        emote = new ExpandoObject();
                        ((dynamic)emote).Expires = DateTime.Now + TimeSpan.FromDays(7);
                        ((dynamic)emote).Id = channelEmote.Id;
                        ((dynamic)emote).Name = channelEmote.Name;
                        ((dynamic)emote).Tier = channelEmote.Tier;
                        ((dynamic)emote).ImageUrl1x = channelEmote.Images.Url1X;
                        ((dynamic)emote).ImageUrl2x = channelEmote.Images.Url2X;
                        ((dynamic)emote).ImageUrl4x = channelEmote.Images.Url4X;
                        await dataService.SetObject($"Twitch.Emotes.{channelEmote.Name}", emote);                        
                    }
                }
                catch (Exception exception)
                {
                    await messagingService.AddMessage($"Error getting channel emotes: {exception.Message}", MessageCategory.Module, LogLevel.Error);
                }

                try
                {
                    GetGlobalEmotesResponse globalEmotesResponse = await twitchService.TwitchAPI.Helix.Chat.GetGlobalEmotesAsync();
                    foreach (GlobalEmote globalEmote in globalEmotesResponse.GlobalEmotes)
                    {
                        emote = new ExpandoObject();
                        ((dynamic)emote).Expires = DateTime.Now + TimeSpan.FromDays(7);
                        ((dynamic)emote).Id = globalEmote.Id;
                        ((dynamic)emote).Name = globalEmote.Name;
                        ((dynamic)emote).Tier = 0;
                        ((dynamic)emote).ImageUrl1x = globalEmote.Images.Url1X;
                        ((dynamic)emote).ImageUrl2x = globalEmote.Images.Url2X;
                        ((dynamic)emote).ImageUrl4x = globalEmote.Images.Url4X;
                        await dataService.SetObject($"Twitch.Emotes.{globalEmote.Name}", emote);
                    }                    
                }
                catch (Exception exception)
                {
                    await messagingService.AddMessage($"Error getting global emotes: {exception.Message}", MessageCategory.Module, LogLevel.Error);
                }

                emote = await dataService.GetObject($"Twitch.Emotes.{emoteKey}");

                if (emote == null)
                {
                    await messagingService.AddMessage("Could not retrieve emote from Twitch.", MessageCategory.Module, LogLevel.Error);
                    return null;
                }
            }

            return emote;
        }
    }
}
