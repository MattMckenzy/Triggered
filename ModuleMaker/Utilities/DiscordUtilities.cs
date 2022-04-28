using Discord.WebSocket;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

namespace ModuleMaker.Utilities
{
    public static class DiscordUtilities
    {
        /// <summary>
        /// Retrieves the configured Discord text channel associated with the Twitch chat.
        /// </summary>
        /// <param name="discordSevice">Instance of <see cref="DiscordService"/> that this extends from.</param>
        /// <param name="triggeredDbContext">An instance of <see cref="TriggeredDbContext"/> used to retrieve configured IDs (key: DiscordGuildId; key: DiscordSyncTextChannelId).</param>
        /// <returns>The related SocketTextChannel, or null of not found.</returns>
        public static SocketTextChannel? GetTwitchTextChannel(this DiscordService discordSevice, TriggeredDbContext triggeredDbContext)
        {
            return discordSevice.DiscordSocketClient.GetGuild(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordGuildId")))?
                .GetTextChannel(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordSyncTextChannelId")));
        }
    }
}
