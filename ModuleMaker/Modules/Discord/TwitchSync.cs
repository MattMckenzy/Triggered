using Microsoft.EntityFrameworkCore;
using ModuleMaker.Utilities;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;
using static Discord.WebSocket.BaseSocketClient;

namespace ModuleMaker.Modules.Discord
{
    public class TwitchSync
    {
        /// <summary>
        /// Sends the received discord message to the Twitch chat, as long as it's from the configured Discord text channel and the message is not from a bot.
        /// </summary>
        public static async Task<bool> SendToTwitch(SocketMessageReceivedArguments eventArgs, DiscordService discordService, TwitchChatService twitchChatService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            if (eventArgs.SocketMessage.Author.IsBot || 
                eventArgs.SocketMessage.Channel.Id != DiscordUtilities.GetTwitchTextChannel(discordService, await triggeredDbContextFactory.CreateDbContextAsync())?.Id)
                return true;

            twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName.ToLower(), $"From Discord user {eventArgs.SocketMessage.Author.Username}:\r\n{eventArgs.SocketMessage.Content}");

            return true;
        }
    }
}