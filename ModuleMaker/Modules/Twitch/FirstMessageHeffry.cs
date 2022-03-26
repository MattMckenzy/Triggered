using OBSWebsocketDotNet.Types;
using System.Threading.Tasks;
using TownBulletin.Services;
using TwitchLib.Client.Events;

namespace TownBulletin.Modules.OnMessageReceived
{
    public class FirstMessageHeffry
    {
        public async static Task<bool> ShowFirstMessageHeffry(OnRitualNewChatterArgs eventArgs, QueueService queueService, ObsService obsService)
        {
            await queueService.Add("ShowFirstMessageHeffry", async () => {

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FirstMessageText");
                properties.Text = $"Welcome to the channel!\r\n{eventArgs.RitualNewChatter.DisplayName}";
                obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                SceneItemProperties sceneItemProperties = obsService.OBSWebsocket.GetSceneItemProperties("FirstMessage", "Animation");
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
