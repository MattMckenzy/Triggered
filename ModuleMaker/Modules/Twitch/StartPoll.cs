using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ModuleMaker.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Hubs;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.SubscriptionTypes.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class PollShower
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> ShowPoll(ChannelPollBeginArgs eventArgs, IHubContext<TriggeredHub> triggeredHub, QueueService queueService, IDbContextFactory<TriggeredDbContext> dbContextFactory, ObsService obsService)
        {
            await queueService.Add("ShowPoll", async () =>
            {
                ChannelPollBegin channelPollBegin = eventArgs.Notification.Event;
                IEnumerable<string> choiceTitles = channelPollBegin.Choices.Select(choice => choice.Title);
                string? speechFilePath = await GoogleUtilities.DetectAndSynthesizeSpeech($"Incoming Poll... {channelPollBegin.Title}. {string.Join(". ", choiceTitles)}", dbContextFactory.CreateDbContext());
                if (speechFilePath != null)
                    _ = OBSSceneUtilities.PlayMediaSource(obsService, speechFilePath, "MediaSound", "MediaOnly", "Animations");

                await triggeredHub.Clients.All.SendAsync("StartPoll",
                    channelPollBegin.Id,  
                    channelPollBegin.Title,
                    choiceTitles,
                    new DateTimeOffset(channelPollBegin.EndsAt).ToUnixTimeMilliseconds());
                
                return true;
            });

            return true;
        }
    }
}