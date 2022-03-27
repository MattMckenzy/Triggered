using System;
using System.Linq;
using TwitchLib.Client.Models.Internal;

namespace TwitchLib.Client.Models
{
    public class BeingHostedNotification
    {
        public string BotUsername { get; set; }

        public string Channel { get; set; }

        public string HostedByChannel { get; set; }

        public bool IsAutoHosted { get; set; }

        public int Viewers { get; set; }

        public BeingHostedNotification(string botUsername, IrcMessage ircMessage)
        {
            Channel = ircMessage.Channel;
            BotUsername = botUsername;
            HostedByChannel = ircMessage.Message.Split(' ').First();

            if (ircMessage.Message.Contains("up to "))
            {
                var splt = ircMessage.Message.Split(new string[] { "up to " }, StringSplitOptions.None);
                if (splt[1].Contains(" ") && int.TryParse(splt[1].Split(' ')[0], out int n))
                    Viewers = n;
            }

            if (ircMessage.Message.Contains("auto hosting"))
                IsAutoHosted = true;
        }

        public BeingHostedNotification(
            string channel,
            string botUsername,
            string hostedByChannel,
            int viewers,
            bool isAutoHosted)
        {
            Channel = channel;
            BotUsername = botUsername;
            HostedByChannel = hostedByChannel;
            Viewers = viewers;
            IsAutoHosted = isAutoHosted;
        }

        public BeingHostedNotification()
        {
        }
    }
}
