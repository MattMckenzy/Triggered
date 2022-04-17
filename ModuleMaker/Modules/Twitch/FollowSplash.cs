using Microsoft.EntityFrameworkCore;
using ModuleMaker.Utilities;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class OnFollow
    {
        public static async Task<bool> ShowFollowSplash(ChannelFollowArgs eventArgs, ObsService obsService, QueueService queueService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            await queueService.Add("TopSplash", async () => {

                TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
                properties.Text = $"Thank you for the follow!\r\nMerci beaucoup pour le suivi!\r\n{eventArgs.Notification.Event.UserName}";
                obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                DirectoryInfo followResourcesDirectory = new(triggeredDbContext.Settings.GetSetting("FollowResourcesDirectory"));

                Random random = new();
                FileInfo[] soundFiles = followResourcesDirectory.GetFiles("*.mp3", SearchOption.AllDirectories);
                FileInfo chosenSound = soundFiles[random.Next(soundFiles.Length)];
                FileInfo[] imageFiles = followResourcesDirectory.GetFiles("*.png", SearchOption.AllDirectories);
                FileInfo chosenImage = imageFiles[random.Next(imageFiles.Length)];

                SourceSettings mediaSourceSettings = obsService.OBSWebsocket.GetSourceSettings("FollowImage", "image_source");
                mediaSourceSettings.Settings["file"] = chosenImage.FullName;
                obsService.OBSWebsocket.SetSourceSettings("FollowImage", mediaSourceSettings.Settings);

                await OBSSceneUtilities.PlayMediaSource(obsService, chosenSound.FullName, "FollowSound", "FollowSplash", "Animations");

                return true;

            });

            return true;
        }
    }
}