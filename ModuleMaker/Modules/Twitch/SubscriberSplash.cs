using OBSWebsocketDotNet.Types;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class SubscriberSplash
    {
        public static async Task<bool> ShowSubscriberSplash(ChannelSubscribeArgs eventArgs, ObsService obsService, QueueService queueService)
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

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("NewFollowText");
                properties.Text = $"Thank you for the {tier} subscription!\r\n{eventArgs.Notification.Event.UserName}";
                obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);
                
                SceneItemProperties textItemProperties = obsService.OBSWebsocket.GetSceneItemProperties("NewFollowText", "Animation");
                int canvasWidth = obsService.OBSWebsocket.GetVideoInfo().OutputWidth;
                textItemProperties.Position.X = (canvasWidth - textItemProperties.Width) / 2;
                textItemProperties.Position =  textItemProperties.Position + textItemProperties.Width;

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