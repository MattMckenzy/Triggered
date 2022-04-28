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
    public class ResubscriberSplash
    {
        /// <summary>
        /// When receiving a resubscription, plays arandom video for the "SubscriptionSplash" scene in OBS.
        /// </summary>
        public static async Task<bool> ShowResubscriberSplash(ChannelSubscriptionMessageArgs eventArgs, ObsService obsService, QueueService queueService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            await queueService.Add("TopSplash", async () => {

                TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("SubscriptionText");
                properties.Text = $"Thank you for resubscribing for {eventArgs.Notification.Event.CumulativeTotal} months!\r\nMerci pour le réabbonnement pendant {eventArgs.Notification.Event.CumulativeTotal} mois!\r\n{eventArgs.Notification.Event.UserName}";
                obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                DirectoryInfo followResourcesDirectory = new(triggeredDbContext.Settings.GetSetting("SubscribeResourcesDirectory"));

                Random random = new();
                FileInfo[] mediaFiles = followResourcesDirectory.GetFiles("*.webm", SearchOption.AllDirectories);
                FileInfo chosenMedia = mediaFiles[random.Next(mediaFiles.Length)];

                await OBSSceneUtilities.PlayMediaSource(obsService, chosenMedia.FullName, "SubscriptionVideo", "SubscriptionSplash", "Animations");

                return true;

            });

            return true;
        }
    }
}