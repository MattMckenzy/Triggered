using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace ModuleMaker.Modules.Twitch
{
    public class SubscriberSplash
    {
        public static async Task<bool> ShowSubscriberSplash(ChannelSubscribeArgs eventArgs, ObsService obsService, QueueService queueService)
        {
            await queueService.Add("TopSplash", async () => {

                int minimumSeconds = 5;
                DateTime minimumTime = DateTime.Now + TimeSpan.FromSeconds(minimumSeconds);

                SceneItemProperties sceneItemProperties = obsService.OBSWebsocket.GetSceneItemProperties("SubscriptionSplash", "Animations");
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

                async void OBSWebsocket_MediaEnded(object sender, MediaEventArgs e)
                {
                    DateTime currentTime = DateTime.Now;
                    if (currentTime < minimumTime)
                        await Task.Delay((int)(minimumTime - currentTime).TotalMilliseconds);

                    sceneItemProperties.Visible = false;
                    obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animations");

                    cancellationTokenSource.Cancel();
                }

                try
                {
                    obsService.OBSWebsocket.MediaEnded += OBSWebsocket_MediaEnded;

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
                    properties.Text = $"Thank you for the {tier} subscription!\r\nMerci pour l'abbonnement de {tierFR}!\r\n{eventArgs.Notification.Event.UserName}";
                    obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                    DirectoryInfo followResourcesDirectory = new("D:\\Streaming\\Animations\\Subscribe");

                    Random random = new();
                    FileInfo[] mediaFiles = followResourcesDirectory.GetFiles("*.webm", SearchOption.AllDirectories);
                    FileInfo chosenMedia = mediaFiles[random.Next(mediaFiles.Length)];

                    MediaSourceSettings followSoundSettings = obsService.OBSWebsocket.GetMediaSourceSettings("SubscriptionVideo");
                    followSoundSettings.Media.LocalFile = chosenMedia.FullName;
                    obsService.OBSWebsocket.SetMediaSourceSettings(followSoundSettings);

                    sceneItemProperties.Visible = true;
                    obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animations");

                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    obsService.OBSWebsocket.MediaEnded -= OBSWebsocket_MediaEnded;
                }


                return true;

            });

            return true;
        }
    }
}