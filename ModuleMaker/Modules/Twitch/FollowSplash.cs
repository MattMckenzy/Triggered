﻿using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace TownBulletin.Modules.OnFollow
{
    public class OnFollow
    {
        public static async Task<bool> ShowFollowSplash(ChannelFollowArgs eventArgs, ObsService obsService, QueueService queueService)
        {
            await queueService.Add("TopSplash", async () => {

                int minimumSeconds = 5;
                DateTime minimumTime = DateTime.Now + TimeSpan.FromSeconds(minimumSeconds);

                SceneItemProperties sceneItemProperties = obsService.OBSWebsocket.GetSceneItemProperties("FollowSplash", "Animations");
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

                    TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
                    properties.Text = $"Thank you for the follow!\r\nMerci beaucoup pour le suivi!\r\n{eventArgs.Notification.Event.UserName}";
                    obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

                    DirectoryInfo followResourcesDirectory = new("D:\\Streaming\\Animations\\Follow");

                    Random random = new();
                    FileInfo[] soundFiles = followResourcesDirectory.GetFiles("*.mp3", SearchOption.AllDirectories);
                    FileInfo chosenSound = soundFiles[random.Next(soundFiles.Length)];
                    FileInfo[] imageFiles = followResourcesDirectory.GetFiles("*.png", SearchOption.AllDirectories);
                    FileInfo chosenImage = imageFiles[random.Next(imageFiles.Length)];

                    SourceSettings mediaSourceSettings = obsService.OBSWebsocket.GetSourceSettings("FollowImage", "image_source");
                    mediaSourceSettings.Settings["file"] = chosenImage.FullName;
                    obsService.OBSWebsocket.SetSourceSettings("FollowImage", mediaSourceSettings.Settings);

                    MediaSourceSettings followSoundSettings = obsService.OBSWebsocket.GetMediaSourceSettings("FollowSound");
                    followSoundSettings.Media.LocalFile = chosenSound.FullName;
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