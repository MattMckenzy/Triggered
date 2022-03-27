using System.Collections.Generic;

namespace TwitchLib.Client.Models
{
    /// <summary>Model representing a sent message.</summary>
    public class SentMessage
    {
        /// <summary>Badges the sender has</summary>
        public List<KeyValuePair<string, string>> Badges { get; set; }

        /// <summary>Channel the sent message was sent from.</summary>
        public string Channel { get; set; }

        /// <summary>Sender's name color.</summary>
        public string ColorHex { get; set; }

        /// <summary>Display name of the sender.</summary>
        public string DisplayName { get; set; }

        /// <summary>Emotes that appear in the sent message.</summary>
        public string EmoteSet { get; set; }

        /// <summary>Whether or not the sender is a moderator.</summary>
        public bool IsModerator { get; set; }

        /// <summary>Whether or not the sender is a subscriber.</summary>
        public bool IsSubscriber { get; set; }

        /// <summary>The message contents.</summary>
        public string Message { get; set; }

        /// <summary>The type of user (admin, broadcaster, viewer, moderator)</summary>
        public Enums.UserType UserType { get; set; }

        /// <summary>Model constructor.</summary>
        public SentMessage(
            UserState state,
            string message)
        {
            Badges = state.Badges;
            Channel = state.Channel;
            ColorHex = state.ColorHex;
            DisplayName = state.DisplayName;
            EmoteSet = state.EmoteSet;
            IsModerator = state.IsModerator;
            IsSubscriber = state.IsSubscriber;
            UserType = state.UserType;
            Message = message;
        }

        public SentMessage(
            List<KeyValuePair<string, string>> badges,
            string channel,
            string colorHex,
            string displayName,
            string emoteSet,
            bool isModerator,
            bool isSubscriber,
            Enums.UserType userType,
            string message)
        {
            Badges = badges;
            Channel = channel;
            ColorHex = colorHex;
            DisplayName = displayName;
            EmoteSet = emoteSet;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            UserType = userType;
            Message = message;
        }

        public SentMessage()
        {
        }
    }
}
