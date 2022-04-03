using OBSWebsocketDotNet.Types;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class GiftSubscriberSplash
    {
        public static async Task<bool> ShowGiftSubscriberSplash(ChannelSubscriptionGiftArgs eventArgs, ObsService obsService, QueueService queueService)
        {
            await queueService.Add("ShowFollowSplash", async () => {

                string tier = "tier";
                switch (eventArgs.Notification.Event.Tier)
                {
                    case "1000":
                    tier += " 1"; break;                    
                    case "2000":
                    tier += " 2"; break;
                    case "3000":
                    tier += " 3"; break;
                }

                string? gifter = eventArgs.Notification.Event.UserName;

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("NewFollow");
                properties.Text = $"Thank you for gifting a {tier} sub!\r\n" +
                $"🎁{gifter ?? "anonymous"}🎁";
                obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                SceneItemProperties sceneItemProperties = obsService.OBSWebsocket.GetSceneItemProperties("NewFollow", "Animation");
                sceneItemProperties.Visible = true;

                obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animation");
                await Task.Delay(5000);

                sceneItemProperties.Visible = false;
                obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animation");

                return true;

            });

            return true;
        }
    }
}