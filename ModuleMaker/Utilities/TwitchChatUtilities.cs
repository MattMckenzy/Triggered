using System.Text.RegularExpressions;
using TwitchLib.Client.Models;
using System.Linq;

namespace ModuleMaker.Utilities
{
    public static class TwitchChatUtilities
    {
        /// <summary>
        /// Replaces the Twitch emotes in the current message string with their URL.
        /// </summary>
        /// <param name="message">The current message string, via extension.</param>
        /// <param name="emoteSet">The EmoteSet used to find and replace emotes with their URL.</param>
        /// <returns>The emote-replaced string.</returns>
        public static string GetTwitchMessageWithUrls(this string message, EmoteSet emoteSet)
        {
            foreach (Emote emote in emoteSet.Emotes.Reverse<Emote>())
            {
                message = message.Remove(emote.StartIndex, emote.EndIndex - emote.StartIndex + 1);
                message = message.Insert(emote.StartIndex, emote.ImageUrl);
            }

            return Regex.Replace(message, @"\s+", " ");
        }

        /// <summary>
        /// Removes the Twitch emotes in the current message string.
        /// </summary>
        /// <param name="message">The current message string, via extension.</param>
        /// <param name="emoteSet">The EmoteSet used to find and remove emotes.</param>
        /// <returns>The emote-removed string.</returns>
        public static string GetTwitchMessageWithoutEmotes(this string message, EmoteSet emoteSet)
        {
            foreach(Emote emote in emoteSet.Emotes.Reverse<Emote>())
                message = message.Remove(emote.StartIndex, emote.EndIndex - emote.StartIndex + 1);

            return Regex.Replace(message, @"\s+", " ");
        }
    }
}
