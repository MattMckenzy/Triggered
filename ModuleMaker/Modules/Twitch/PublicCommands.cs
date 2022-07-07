using System.Threading.Tasks;
using TwitchLib.Client.Events;
using ModuleMaker.Utilities;
using Triggered.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using Microsoft.Extensions.Caching.Memory;
using Triggered.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class PublicCommands
    {
        public static async Task<bool> ParseCommand(OnChatCommandReceivedArgs eventArgs, IHubContext<TriggeredHub> hubContext, MemoryCache memoryCache, TwitchChatService twitchChatService)
        {
            switch (eventArgs.Command.CommandText)
            {
                case string commandText when commandText.Equals("play", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.PlayMediaPlayer(hubContext);
                    break;

                case string commandText when commandText.Equals("pause", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.PauseMediaPlayer(hubContext);
                    break;

                case string commandText when commandText.Equals("next", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.NextMedia(hubContext);
                    break;

                case string commandText when commandText.Equals("previous", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.PreviousMedia(hubContext);
                    break;

                case string commandText when commandText.Equals("shuffle", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.ShuffleMedia(hubContext);
                    break;

                case string commandText when commandText.Equals("select", StringComparison.InvariantCultureIgnoreCase):
                    await MediaPlayerUtilities.SelectMedia(hubContext, eventArgs.Command.ArgumentsAsString);
                    break;

                case string commandText when commandText.Equals("playlist", StringComparison.InvariantCultureIgnoreCase):
                    twitchChatService.TwitchClient.SendMessage("MattMckenzy", MediaPlayerUtilities.GetMediaList(memoryCache));
                    break;
            }

            return true;
        }
    }
}
