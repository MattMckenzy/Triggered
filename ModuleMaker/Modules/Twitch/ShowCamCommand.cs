using System.Threading.Tasks;
using System;
using TwitchLib.Client.Events;
using TownBulletin.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class ShowCamCommand
    {
        public static async Task<bool> ShowCam(OnChatCommandReceivedArgs onChatCommandReceivedArgs, ObsService obsService)
        {
            if (onChatCommandReceivedArgs.Command.CommandText.Equals("camonly", StringComparison.InvariantCultureIgnoreCase))
            {
                string currentScene = obsService.OBSWebsocket.GetCurrentScene().Name;
                obsService.OBSWebsocket.SetCurrentScene("Full Camera");
                await Task.Delay(5000);
                obsService.OBSWebsocket.SetCurrentScene(currentScene);
            }

            return true;
        }
    }
}