using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ModuleMaker.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class StoryPollEnder
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> EndPoll(ChannelPollEndArgs eventArgs, QueueService queueService, IDbContextFactory<TriggeredDbContext> contextFactory, MemoryCache memoryCache, MessagingService messagingService)
        {
            await queueService.Add("ShowPoll", async () =>
            {
                TriggeredDbContext triggeredDbContext = await contextFactory.CreateDbContextAsync();

                if (triggeredDbContext.Settings.GetSetting($"CurrentPoll").Equals(eventArgs.Notification.Event.Id, StringComparison.InvariantCultureIgnoreCase))
                {
                    await TwineUtilities.SetNextPassageId(
                        triggeredDbContext.Settings.GetSetting($"CurrentStory"),
                        eventArgs.Notification.Event.Choices.OrderByDescending(choice => choice.Votes).First().Title,
                        memoryCache,
                        triggeredDbContext,
                        messagingService);

                    triggeredDbContext.Settings.SetSetting($"CurrentPoll", String.Empty);
                };

                return true;
            });

            return true;
        }
    }
}