using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream;

namespace ModuleMaker.Modules.Twitch
{
    public class StreamAnnounce
    {
        public static async Task<bool> AnnounceTriggered(StreamOnlineArgs eventArgs, TwitchService twitchService, TwitchChatService twitchChatService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

            ChannelInformation? channelInformation = (await twitchService.TwitchAPI.Helix.Channels.GetChannelInformationAsync(twitchService.User!.Id)).Data.FirstOrDefault();

            if (channelInformation != null &&
                (channelInformation.Title.Contains("tr⚡ggered", System.StringComparison.InvariantCultureIgnoreCase) ||
                    channelInformation.Title.Contains("triggered", System.StringComparison.InvariantCultureIgnoreCase)))
            {
                twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, triggeredDbContext.Settings.GetSetting("StartTriggeredStreamAnnouncement"));
            }

            if (channelInformation != null)
                twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, triggeredDbContext.Settings.GetSetting("StreamStartAnnouncement"));

            return true;
        }
    }
}