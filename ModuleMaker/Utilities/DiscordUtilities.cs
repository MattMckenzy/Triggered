using Discord.WebSocket;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

namespace ModuleMaker.Utilities
{
    public static class DiscordUtilities
    {
        public static SocketTextChannel? GetTwitchTextChannel(this DiscordService discordSevice, TriggeredDbContext triggeredDbContext)
        {
            return discordSevice.DiscordClient.GetGuild(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordGuildId")))?
                .GetTextChannel(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordSyncTextChannelId")));
        }
    }
}
