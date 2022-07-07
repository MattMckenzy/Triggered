using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Triggered.Hubs;

namespace ModuleMaker.Utilities
{
    public class MediaPlayerUtilities
    {
        public static async Task PlayMediaPlayer(IHubContext<TriggeredHub> triggeredHub)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerPlay");
        }

        public static async Task PauseMediaPlayer(IHubContext<TriggeredHub> triggeredHub)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerPause");
        }

        public static async Task NextMedia(IHubContext<TriggeredHub> triggeredHub)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerNext");
        }

        public static async Task PreviousMedia(IHubContext<TriggeredHub> triggeredHub)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerPrevious");
        }

        public static async Task ShuffleMedia(IHubContext<TriggeredHub> triggeredHub)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerShuffle");
        }

        public static async Task SelectMedia(IHubContext<TriggeredHub> triggeredHub, string searchText)
        {
            await triggeredHub.Clients.All.SendAsync("MediaPlayerSelect", searchText);
        }

        public static string GetMediaList(MemoryCache memoryCache)
        {
            return memoryCache.Get<string>("MediaPlayerList");
        }
    }
}