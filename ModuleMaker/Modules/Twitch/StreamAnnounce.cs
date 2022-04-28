using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
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
        private static CancellationTokenSource? CancellationTokenSource { get; set; }

        /// <summary>
        /// When the stream goes online, announces configured start stream announcements, including a special one if triggered is part of the title.
        /// </summary>
        public static async Task<bool> AnnounceTriggered(StreamOnlineArgs eventArgs, TwitchService twitchService, TwitchChatService twitchChatService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory, LongRunningTask longRunningTask)
        {
            TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

            ChannelInformation? channelInformation = (await twitchService.TwitchAPI.Helix.Channels.GetChannelInformationAsync(twitchService.User!.Id)).Data.FirstOrDefault();

            if (CancellationTokenSource != null)
                CancellationTokenSource.Cancel();

            CancellationTokenSource = longRunningTask.CancellationTokenSource;
            twitchService.TwitchEventSubWebhooks.OnStreamOffline += TwitchEventSubWebhooks_OnStreamOffline;

            longRunningTask.Task = Task.Run(async () =>
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    if (channelInformation != null &&
                    (channelInformation.Title.Contains("tr⚡ggered", StringComparison.InvariantCultureIgnoreCase) ||
                        channelInformation.Title.Contains("triggered", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, triggeredDbContext.Settings.GetSetting("StartTriggeredStreamAnnouncement"));
                    }

                    if (channelInformation != null)
                        twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, triggeredDbContext.Settings.GetSetting("StreamStartAnnouncement"));

                    await Task.Delay(TimeSpan.FromMinutes(30), CancellationTokenSource.Token);
                }
            },
            CancellationTokenSource.Token);

            return true;
        }

        private static void TwitchEventSubWebhooks_OnStreamOffline(object? sender, StreamOfflineArgs e)
        {
            CancellationTokenSource?.Cancel();
        }
    }
}