using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using ModuleMaker.Utilities;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Client.Events;

namespace ModuleMaker.Modules.Discord
{
    public class DiscordSync
    {
        public static async Task<bool> SendToDiscord(OnMessageReceivedArgs eventArgs, DiscordService discordSevice, DataService dataService, TwitchService twitchService, TwitchChatService twitchChatService, MessagingService messagingService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            if (eventArgs.ChatMessage.Username.Equals(twitchChatService.UserName, StringComparison.InvariantCultureIgnoreCase) && eventArgs.ChatMessage.Message.StartsWith("From Discord user"))
                return true;

            SocketTextChannel? channel = discordSevice.GetTwitchTextChannel(await triggeredDbContextFactory.CreateDbContextAsync());

            if (channel != null)
            {
                ExpandoObject? user = await TwitchUserUtilities.GetTwitchUser(dataService, twitchService, messagingService, eventArgs.ChatMessage.UserId);

                if (user != null)
                {
                    await channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithUrl($"https://twitch.tv/{twitchService.ChannelName}")
                        .WithColor(Color.DarkTeal)
                        .WithDescription(eventArgs.ChatMessage.Message)
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(eventArgs.ChatMessage.DisplayName)
                            .WithUrl($"https://twitch.tv/{(string)((dynamic)user).Login}")
                            .WithIconUrl((string)((dynamic)user).ProfileImageUrl))
                        .Build());
                }               
            }

            return true;
        }
    }
}
#nullable disable