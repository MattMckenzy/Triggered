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
    public class GiftSubscriberSplash
    {
        public static async Task<bool> ShowGiftSubscriberSplash(ChannelSubscriptionGiftArgs eventArgs, ObsService obsService, QueueService queueService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            await queueService.Add("TopSplash", async () => {

                TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

                string tier = "TIER";
                string tierFR = "NIVEAU";
                switch (eventArgs.Notification.Event.Tier)
                {
                    case "1000":
                        tier += " 1"; tierFR += " 1"; break;
                    case "2000":
                        tier += " 2"; tierFR += " 2"; break;
                    case "3000":
                        tier += " 3"; tierFR += " 3"; break;
                }

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("SubscriptionText");
                properties.Text = $"Thank you for gifting a {tier} subscription!\r\nMerci pour donner un abbonnement de {tierFR}!\r\n🎁 {eventArgs.Notification.Event.UserName} 🎁";
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