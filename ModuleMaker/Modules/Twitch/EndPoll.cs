using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Hubs;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.SubscriptionTypes.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class PollEnder
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> EndPoll(ChannelPollEndArgs eventArgs, IHubContext<TriggeredHub> triggeredHub, QueueService queueService)
        {
            await queueService.Add("EndPoll", async () =>
            {
                ChannelPollEnd channelPollEnd = eventArgs.Notification.Event;
                int winningVoteCount = channelPollEnd.Choices.Max(choice => choice.Votes) ?? 0;
                await triggeredHub.Clients.All.SendAsync(
                    "EndPoll",
                    channelPollEnd.Id,
                    channelPollEnd.Choices.Sum(choice => choice.Votes),
                    winningVoteCount,
                    channelPollEnd.Choices.Where(choice => choice.Votes == winningVoteCount)
                        .Select(choice => $"#{Array.IndexOf(channelPollEnd.Choices, choice) + 1}. {choice.Title}"));

                return true;
            });

            return true;
        }
    }
}