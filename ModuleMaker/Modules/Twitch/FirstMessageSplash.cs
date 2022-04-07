using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Triggered.Services;
using TwitchLib.Client.Events;

namespace ModuleMaker.Modules.Twitch
{
    public class FirstMessageSplash
    {
        public async static Task<bool> ShowFirstMessageHeffry(OnRitualNewChatterArgs eventArgs, QueueService queueService, ObsService obsService, TwitchChatService twitchChatService)
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

                    twitchChatService.TwitchClient.SendMessage("mattmckenzy", $"Welcome to MattMckenzy's Channel, {eventArgs.RitualNewChatter.DisplayName}! Please be courteous and treat everyone with respect. However, feel free to interrupt him and ask him anything, he loves answering questions!");

                    twitchChatService.TwitchClient.SendMessage("mattmckenzy", $"Bienvenue à la chaîne de MattMckenzy, {eventArgs.RitualNewChatter.DisplayName}! S'il vous plaît, restez poli et respectueux. Sentez-vous libre de l'interrompre avec tous vos questions, il adore jaser!");

                    TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
                    properties.Text = $"Welcome to the channel!\r\nBienvenue à la chaîne!\r\n{eventArgs.RitualNewChatter.DisplayName}";
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