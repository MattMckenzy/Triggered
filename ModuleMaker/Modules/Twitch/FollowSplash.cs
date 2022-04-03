using OBSWebsocketDotNet.Types;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class FollowSplash
    {
        public static async Task<bool> ShowFollowSplash(ChannelFollowArgs eventArgs, ObsService obsService, QueueService queueService)
        {
            await queueService.Add("ShowFollowSplash", async () => {

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("NewFollow");
                properties.Text = $"Thank you for the follow!\r\n{eventArgs.Notification.Event.UserName}";
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