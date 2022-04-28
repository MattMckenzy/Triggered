using Microsoft.EntityFrameworkCore;
using ModuleMaker.Utilities;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Client.Events;

namespace ModuleMaker.Modules.Twitch
{
    public class MessageTTS
    {
        /// <summary>
        /// When receiving a highlighted or cheer message, detects its language, translates it if not english or french, synthesizes speech from it and plays it in the "FollowSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> SpeakMessage(OnMessageReceivedArgs eventArgs, ObsService obsService, QueueService queueService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            if (eventArgs.ChatMessage.IsHighlighted || eventArgs.ChatMessage.Bits > 0)
            {
                await queueService.Add("TopSplash", async () =>
                {
                    TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

                    long wavName = DateTime.Now.Ticks;
                    string message = eventArgs.ChatMessage.Message.GetTwitchMessageWithoutEmotes(eventArgs.ChatMessage.EmoteSet);

                    TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
                    properties.Text =
                        eventArgs.ChatMessage.IsHighlighted ? 
                            $"{eventArgs.ChatMessage.DisplayName}'s highlighted message!\r\n{message}" :
                            $"{eventArgs.ChatMessage.DisplayName}'s cheer: {eventArgs.ChatMessage.Bits} bits!\r\n{message}";
                    obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                    DirectoryInfo followResourcesDirectory = new(triggeredDbContext.Settings.GetSetting("FollowResourcesDirectory"));

                    Random random = new();
                    FileInfo[] imageFiles = followResourcesDirectory.GetFiles("*.png", SearchOption.AllDirectories);
                    FileInfo chosenImage = imageFiles[random.Next(imageFiles.Length)];

                    SourceSettings mediaSourceSettings = obsService.OBSWebsocket.GetSourceSettings("FollowImage", "image_source");
                    mediaSourceSettings.Settings["file"] = chosenImage.FullName;
                    obsService.OBSWebsocket.SetSourceSettings("FollowImage", mediaSourceSettings.Settings);

                    string? speechFilePath = await GoogleUtilities.DetectAndSynthesizeSpeech(message, triggeredDbContext);
                    if (speechFilePath != null)
                    {
                        await OBSSceneUtilities.PlayMediaSource(obsService, speechFilePath, "FollowSound", "FollowSplash", "Animations");
                    }

                    return true;
                });
            }

            return true;
        }
    }
}