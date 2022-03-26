using OBSWebsocketDotNet;
using TownBulletin.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class AnnounceSpotify
    {
        public static Task<bool> SpotifyAnnounce(SceneItemVisibilityEventArgs sceneItemVisibilityEventArgs, TwitchBotService twitchBotService)
        {
            if (sceneItemVisibilityEventArgs.ItemName.Equals("Spotify", StringComparison.InvariantCultureIgnoreCase) && sceneItemVisibilityEventArgs.IsVisible)
            {
                twitchBotService.TwitchClient.SendMessage(twitchBotService.ChannelName, "Have a look at the currently playing track from Spotify on the bottom left of the stream!");
            }

            return Task.FromResult(true);
        }
    }
}
