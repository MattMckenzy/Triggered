using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModuleMaker.Utilities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Hubs;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Api.Helix.Models.Polls.CreatePoll;

namespace ModuleMaker.Modules.Custom
{
    public class StoryPollCreator
    {
        private const int StorySeconds = 30;

        public static async Task<bool> NextPoll(CustomEventArgs eventArgs, QueueService queueService, TwitchService twitchService, IHubContext<TriggeredHub> triggeredHub, IDbContextFactory<TriggeredDbContext> dbFactory, MemoryCache memoryCache, MessagingService messagingService)
        {
            await queueService.Add("ShowPoll", async () =>
            {
                if (eventArgs.Identifier?.Equals("StartStory", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    TriggeredDbContext triggeredDbContext = await dbFactory.CreateDbContextAsync();

                    if (!string.IsNullOrWhiteSpace(triggeredDbContext.Settings.GetSetting($"CurrentPoll")))
                        return true;

                    string? currentStory = eventArgs.Data?.ToString();
                    if (string.IsNullOrWhiteSpace(currentStory))
                    {
                        await messagingService.AddMessage($"The passed in data event argument \"Data\" is empty! Please set it to an existing Twine story saved in the data service.", MessageCategory.Module, LogLevel.Error);
                        return true;
                    }

                    int? nextPollId = null;
                    if (int.TryParse(triggeredDbContext.Settings.GetSetting($"{currentStory}.NextPassageId"), out int parsedPollId))
                        nextPollId = parsedPollId;

                    TwinePassage? nextPoll = await TwineUtilities.GetPassage(currentStory, nextPollId, memoryCache, triggeredDbContext, messagingService);
                    if (nextPoll != null)
                    {
                        Choice[] choices = nextPoll.Choices.OrderBy(keyValuePairs => keyValuePairs.Key).Select(keyValuePairs => new Choice { Title = keyValuePairs.Value }).ToArray();

                        triggeredDbContext.Settings.SetSetting($"{currentStory}.NextPassageId", nextPoll.PassageId.ToString());
                        triggeredDbContext.Settings.SetSetting($"CurrentStory", currentStory);

                        if (choices.Length == 0)
                        {
                            triggeredDbContext.Settings.SetSetting($"{currentStory}.NextPassageId", string.Empty);
                            triggeredDbContext.Settings.SetSetting($"CurrentStory", string.Empty);
                        }
                        else if (choices.Length == 1)
                        {
                            await TwineUtilities.SetNextPassageId(
                                currentStory,
                                choices.First().Title,
                                memoryCache,
                                triggeredDbContext,
                                messagingService);
                        }
                        else
                        {
                            foreach (Choice choice in choices)
                                if (choice.Title.Length > 25)
                                    choice.Title = string.Concat(choice.Title.AsSpan(0, 22), "...");

                            CreatePollResponse createPollResponse = await twitchService.TwitchAPI.Helix.Polls.CreatePoll(new CreatePollRequest
                            {
                                BitsVotingEnabled = false,
                                BroadcasterId = twitchService.User!.Id,
                                DurationSeconds = StorySeconds,
                                Title = nextPoll.Title.Length > 60 ? string.Concat(nextPoll.Title.AsSpan(0, 57), "...") : nextPoll.Title,
                                Choices = choices
                            });

                            triggeredDbContext.Settings.SetSetting($"CurrentPoll", createPollResponse.Data.First().Id);
                        }

                        await triggeredHub.Clients.All.SendAsync("ShowStory",
                            nextPoll.Title,
                            Regex.Replace(nextPoll.Text, @"\[.*\]|<.*>", string.Empty),
                            nextPoll.Images.FirstOrDefault(),
                            new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(StorySeconds)).ToUnixTimeMilliseconds());
                    }
                }

                return true;
            });

            return true;
        } 
    } 
}