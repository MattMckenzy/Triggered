using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

namespace ModuleMaker.Utilities
{
    public static class TwineUtilities
    {
        public static async Task<TwinePassage?> GetPassage(string twineStoryKey, int? passageId, MemoryCache memoryCache, TriggeredDbContext triggeredDbContext, MessagingService messagingService)
        {
            if (!memoryCache.TryGetValue(twineStoryKey, out TwineStory twineStory))
            {
                DataObject? twineStoryDataObject = await triggeredDbContext.DataObjects.FirstOrDefaultAsync(dataObject => dataObject.Key.Equals(twineStoryKey));
                if (twineStoryDataObject == null || twineStoryDataObject.ExpandoObjectJson == null)
                {
                    await messagingService.AddMessage($"The given key \"{twineStoryKey}\" was not found among the data objects.", MessageCategory.Utility, LogLevel.Error);
                    return null;
                }
                
                JToken twineStoryToken = JObject.Parse(twineStoryDataObject.ExpandoObjectJson);

                twineStory = new TwineStory(twineStoryKey)
                {
                    Name = twineStoryToken["name"]?.Value<string>() ?? string.Empty,
                    StartingPassage = int.TryParse(twineStoryToken["startnode"]?.Value<string>() ?? "1", out int startNode) ? startNode : 1
                };

                JToken? passagesToken = twineStoryToken.SelectToken("passages");
                if (passagesToken != null)
                {
                    foreach (JToken passageToken in passagesToken)
                    {
                        if (!int.TryParse(passageToken["pid"]?.Value<string>() ?? string.Empty, out int newPassageId))
                        {
                            await messagingService.AddMessage($"The story json for \"{twineStoryKey}\" is malformed, there is a passage with no ID.", MessageCategory.Utility, LogLevel.Error);
                            return null;
                        }

                        TwinePassage newTwinePassage = new(newPassageId)
                        {
                            Title = passageToken["name"]?.Value<string>() ?? string.Empty,
                            Text = passageToken["text"]?.Value<string>() ?? string.Empty
                        };

                        JToken? linksToken = passageToken.SelectToken("links");
                        if (linksToken != null)
                        {
                            foreach (JToken choiceToken in linksToken)
                            {
                                if (!int.TryParse(choiceToken["pid"]?.Value<string>() ?? string.Empty, out int newChoiceId))
                                {
                                    await messagingService.AddMessage($"The story json for \"{twineStoryKey}\" is malformed, there is a link with no ID.", MessageCategory.Utility, LogLevel.Error);
                                    return null;
                                }

                                newTwinePassage.Choices.Add(newChoiceId, choiceToken["name"]?.Value<string>() ?? string.Empty);
                            }
                        }

                        JToken? imagesToken = passageToken.SelectToken("images");
                        if (imagesToken != null)
                        {
                            foreach (JToken imageToken in imagesToken)
                            {
                                newTwinePassage.Images.Add(imageToken.Value<string>() ?? string.Empty);
                            }
                        }

                        JToken? tagsToken = passageToken.SelectToken("tags");
                        if (tagsToken != null)
                        {
                            foreach (JToken tagToken in tagsToken)
                            {
                                newTwinePassage.Tags.Add(tagToken.Value<string>() ?? string.Empty);
                            }
                        }

                        twineStory.Passages.Add(newPassageId, newTwinePassage);
                    }
                }

                memoryCache.Set(twineStoryKey, twineStory);
            }

            if (passageId == null)
                passageId = twineStory.StartingPassage;

            if (!twineStory.Passages.TryGetValue((int)passageId, out TwinePassage? twinePassage) || twinePassage == null)
            {
                await messagingService.AddMessage($"The given passageId \"{passageId}\" for \"{twineStoryKey}\" was not found among the passages.", MessageCategory.Utility, LogLevel.Error);
                return null;
            }

            return twinePassage;
        }

        public static async Task SetNextPassageId(string twineStoryKey, string choiceText, MemoryCache memoryCache, TriggeredDbContext triggeredDbContext, MessagingService messagingService)
        {
            if (!string.IsNullOrWhiteSpace(twineStoryKey))
            {
                int? nextPollId = null;
                if (int.TryParse(triggeredDbContext.Settings.GetSetting($"{twineStoryKey}.NextPassageId"), out int parsedPollId))
                {
                    nextPollId = parsedPollId;
                    TwinePassage? twinePassage = await GetPassage(twineStoryKey, nextPollId, memoryCache, triggeredDbContext, messagingService);

                    if (twinePassage != null)
                    {
                        KeyValuePair<int, string> choice = twinePassage.Choices.FirstOrDefault(keyValuePair => keyValuePair.Value.Equals(choiceText, StringComparison.InvariantCultureIgnoreCase));

                        if (!choice.Equals(default(KeyValuePair<int, string>)))
                        {
                            triggeredDbContext.Settings.SetSetting($"{twineStoryKey}.NextPassageId", choice.Key.ToString());
                            return;
                        }
                    }
                }

                triggeredDbContext.Settings.SetSetting($"{twineStoryKey}.NextPassageId", string.Empty);
            }

            triggeredDbContext.Settings.SetSetting($"CurrentStory", string.Empty);
        }
    }

    public class TwineStory
    {
        public TwineStory(string key)
        {
             Key = key;
        }

        public string Key { get; set; }
        public string? Name { get; set; }
        public int StartingPassage { get; set; }
        public Dictionary<int, TwinePassage> Passages { get; set; } = new Dictionary<int, TwinePassage>();
    }

    public class TwinePassage
    {
        public TwinePassage(int passageId)
        {
            PassageId = passageId;
        }

        public int PassageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Dictionary<int, string> Choices { get; set; } = new Dictionary<int, string>();
        public List<string> Images { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
    }
}
