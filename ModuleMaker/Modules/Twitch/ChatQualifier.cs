using System.Threading.Tasks;
using System;
using TwitchLib.Client.Events;
using TownBulletin.Services;

namespace ModuleMaker.Modules.Twitch
{
    public class ChatQualifier
    {
        public static Task<bool> QualifyMessage(OnMessageReceivedArgs onMessageReceivedArgs, TwitchBotService twitchBotService)
        {
            if(onMessageReceivedArgs.ChatMessage.Username.Equals("bigtanger", StringComparison.InvariantCultureIgnoreCase))
            {
                twitchBotService.TwitchClient.SendMessage(twitchBotService.ChannelName, "Merci pour le beau message bigtanger!");
            }

            return Task.FromResult(true);
        }
    }
}
