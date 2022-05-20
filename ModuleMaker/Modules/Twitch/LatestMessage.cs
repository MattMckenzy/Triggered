using Microsoft.AspNetCore.SignalR;
using ModuleMaker.Utilities;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Hubs;
using Triggered.Services;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ModuleMaker.Modules.Twitch
{
    public class LatestMessage
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> ShowLatestMessage(OnMessageReceivedArgs eventArgs, IHubContext<TriggeredHub> triggeredHub, QueueService queueService, DataService dataService, TwitchService twitchService, MessagingService messagingService)
        {
            await queueService.Add("ShowLatestMessage", async () =>
            {
                ExpandoObject? user = await TwitchUserUtilities.GetTwitchUser(dataService, twitchService, messagingService, eventArgs.ChatMessage.UserId);

                if (user != null)
                {
                    string userName = $"<span style=\"color:{eventArgs.ChatMessage.ColorHex};\">{eventArgs.ChatMessage.DisplayName}:</span>";
                    foreach (KeyValuePair<string, string> keyValuePair in eventArgs.ChatMessage.Badges.Reverse<KeyValuePair<string, string>>())
                    {
                        ExpandoObject? badge = await TwitchBadgeUtilities.GetTwitchBadge(dataService, twitchService, messagingService, $"{keyValuePair.Key}.{keyValuePair.Value}");

                        if (badge == null)
                            continue;

                        userName = $"<img class=\"mr-1\" src=\"{(string)((dynamic)badge).ImageUrl1x}\">{userName}";
                    }

                    string message = eventArgs.ChatMessage.Message;
                    foreach (Emote emote in eventArgs.ChatMessage.EmoteSet.Emotes)
                    {
                        ExpandoObject? emoteObject = await TwitchEmoteUtilities.GetTwitchEmote(dataService, twitchService, messagingService, emote.Name);

                        if (emoteObject == null)
                            continue;

                        string emoteUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{((dynamic)emoteObject).Id}/default/light/1.0";
                        message = message.Replace(emote.Name, $"<img height=\"20\" src=\"{emoteUrl}\">");
                    }

                    await triggeredHub.Clients.All.SendAsync("UpdateLastMessage", (string)((dynamic)user).ProfileImageUrl, userName, message);
                }

                return true;
            });

            return true;
        }
    }
}