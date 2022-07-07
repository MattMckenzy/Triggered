using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Hubs;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.SubscriptionTypes.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class PollUpdater
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> UpdatePoll(ChannelPollProgressArgs eventArgs, IHubContext<TriggeredHub> triggeredHub, QueueService queueService)
        {
            await queueService.Add("UpdatePoll", async () =>
            {
                ChannelPollProgress channelPollProgress = eventArgs.Notification.Event;
                await triggeredHub.Clients.All.SendAsync("UpdatePoll",
                    channelPollProgress.Id,
                    channelPollProgress.Choices.Sum(choice => choice.Votes),
                    channelPollProgress.Choices.Select(choice => choice.Votes));

                return true;
            });

            return true;
        }
    }
}