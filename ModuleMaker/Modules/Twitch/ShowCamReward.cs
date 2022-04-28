using System;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class CamOnlyReward
    {
        /// <summary>
        /// Shows the "Full Screen Cam" scene in OBS for 10 seconds.
        /// </summary>
        public static async Task<bool> ShowCamReward(ChannelPointsCustomRewardRedemptionArgs eventArgs, QueueService queueService, ObsService obsService)
        {
            if (!eventArgs.Notification.Event.Reward.Title.Equals("Full Screen Cam", StringComparison.InvariantCultureIgnoreCase))
                return true;

            await queueService.Add("SceneChange", async () =>
            {
                string currentScene = obsService.OBSWebsocket.GetCurrentScene().Name;
                obsService.OBSWebsocket.SetCurrentScene("Full Camera");
                await Task.Delay(10000);
                obsService.OBSWebsocket.SetCurrentScene(currentScene);

                return true;
            });

            return false;
        }
    }
}