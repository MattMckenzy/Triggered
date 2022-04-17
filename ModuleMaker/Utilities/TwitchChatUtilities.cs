using System.Text.RegularExpressions;
using TwitchLib.Client.Models;
using System.Linq;

namespace ModuleMaker.Utilities
{
    public static class TwitchChatUtilities
    {
        public static string GetTwitchMessageWithUrls(this string message, EmoteSet emoteSet)
        {
            foreach (Emote emote in emoteSet.Emotes.Reverse<Emote>())
            {
                message = message.Remove(emote.StartIndex, emote.EndIndex - emote.StartIndex + 1);
                message = message.Insert(emote.StartIndex, emote.ImageUrl);
            }

            return Regex.Replace(message, @"\s+", " ");
        }

        public static string GetTwitchMessageWithoutEmotes(this string message, EmoteSet emoteSet)
        {
            foreach(Emote emote in emoteSet.Emotes.Reverse<Emote>())
                message = message.Remove(emote.StartIndex, emote.EndIndex - emote.StartIndex + 1);

            return Regex.Replace(message, @"\s+", " ");
        }
    }
}
