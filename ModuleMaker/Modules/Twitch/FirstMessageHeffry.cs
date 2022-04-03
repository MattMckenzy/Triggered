using OBSWebsocketDotNet.Types;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.Client.Events;

namespace ModuleMaker.Modules.Twitch
{
    public class FirstMessageHeffry
    {
        public async static Task<bool> ShowFirstMessageHeffry(OnRitualNewChatterArgs eventArgs, QueueService queueService, ObsService obsService, TwitchChatService twitchChatService)
        {
            await queueService.Add("ShowFirstMessageHeffry", async () => {

                twitchChatService.TwitchClient.SendMessage("MattMckenzy", $"Welcome to MattMckenzy's Channel, {eventArgs.RitualNewChatter.DisplayName}! Please be courteous and treat everyone with respect. Feel free to interrupt me and ask me anything anytime!");

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
