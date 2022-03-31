using OBSWebsocketDotNet;
using System;
using System.Threading.Tasks;
using TownBulletin.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class AnnounceSpotify
    {
        public static Task<bool> SpotifyAnnounce(SceneItemVisibilityEventArgs sceneItemVisibilityEventArgs, TwitchChatService twitchChatService)
        {
            if (sceneItemVisibilityEventArgs.ItemName.Equals("Spotify", StringComparison.InvariantCultureIgnoreCase) && sceneItemVisibilityEventArgs.IsVisible)
            {
                twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, "Have a look at the currently playing track from Spotify on the bottom left of the stream!");
            }

            return Task.FromResult(true);
        }
    }
}
