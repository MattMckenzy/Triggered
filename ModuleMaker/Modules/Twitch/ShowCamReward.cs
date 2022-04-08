using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace TownBulletin.Modules.OnRewardRedeemed
{
    public class CamOnlyReward
    {
        public static async Task<bool> ShowCamReward(ChannelPointsCustomRewardRedemptionArgs eventArgs, QueueService queueService, ObsService obsService, TwitchService twitchService)
        {
            if (!eventArgs.Notification.Event.Reward.Title.Equals("Full Screen Cam", StringComparison.InvariantCultureIgnoreCase))
                return true;

            await queueService.Add("SceneChange", async () =>
            {
                string currentScene = obsService.OBSWebsocket.GetCurrentScene().Name;
                obsService.OBSWebsocket.SetCurrentScene("Full Camera");
                await Task.Delay(10000);
                obsService.OBSWebsocket.SetCurrentScene(currentScene);

                await twitchService.TwitchAPI.Helix.ChannelPoints.UpdateCustomRewardRedemptionStatus(
                    eventArgs.Notification.Event.BroadcasterUserId,
                    eventArgs.Notification.Event.Reward.Id, 
                    new List<string> { eventArgs.Notification.Event.Id },
                    new UpdateCustomRewardRedemptionStatusRequest { Status = CustomRewardRedemptionStatus.FULFILLED });

                return true;
            });

            return false;
        }
    }
}