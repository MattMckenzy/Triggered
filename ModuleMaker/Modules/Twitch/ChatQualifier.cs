using System;
using System.Threading.Tasks;
using TownBulletin.Services;
using TwitchLib.Client.Events;

namespace ModuleMaker.Modules.Twitch
{
    public class ChatQualifier
    {
        public static Task<bool> QualifyMessage(OnMessageReceivedArgs onMessageReceivedArgs, TwitchChatService twitchChatService)
        {
            if(onMessageReceivedArgs.ChatMessage.Username.Equals("bigtanger", StringComparison.InvariantCultureIgnoreCase))
            {
                twitchChatService.TwitchClient.SendMessage(twitchChatService.ChannelName, "Merci pour le beau message bigtanger!");
            }

            return Task.FromResult(true);
        }
    }
}
