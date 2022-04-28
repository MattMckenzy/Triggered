using OBSWebsocketDotNet;
using System;
using System.Threading.Tasks;
using Triggered.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class AnnounceSpotify
    {
        /// <summary>
        /// Announces that the "Spotify-Source" scene item becomes visible in the Twitch chat.
        /// </summary>
        public static Task<bool> SpotifyAnnounce(SceneItemVisibilityEventArgs sceneItemVisibilityEventArgs, TwitchChatService twitchChatService)
        {
            if (sceneItemVisibilityEventArgs.ItemName.Equals("Spotify-Source", StringComparison.InvariantCultureIgnoreCase) && sceneItemVisibilityEventArgs.IsVisible)
            {
                twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, "/announce Have a look at the currently playing track from Spotify on the bottom left of the stream!");
            }

            return Task.FromResult(true);
        }
    }
}
