using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Threading;
using System.Threading.Tasks;
using Triggered.Services;

namespace ModuleMaker.Utilities
{
    public static class OBSSceneUtilities
    {
        public static async Task PlayMediaSource(ObsService obsService, string mediaSourcePath, string mediaItemName, string visibilityItemName, string sceneName, int minimumSeconds = 5, int secondsToCancel = 300)
        {
            DateTime minimumTime = DateTime.Now + TimeSpan.FromSeconds(minimumSeconds);

            SceneItemProperties sceneItemProperties = obsService.OBSWebsocket.GetSceneItemProperties(visibilityItemName, sceneName);
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(secondsToCancel));

            async void OBSWebsocket_MediaEnded(object? _, MediaEventArgs __)
            {
                DateTime currentTime = DateTime.Now;
                if (currentTime < minimumTime)
                    await Task.Delay((int)(minimumTime - currentTime).TotalMilliseconds);

                sceneItemProperties.Visible = false;
                obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, sceneName);

                cancellationTokenSource.Cancel();
            }

            try
            {
                obsService.OBSWebsocket.MediaEnded += OBSWebsocket_MediaEnded;

                MediaSourceSettings followSoundSettings = obsService.OBSWebsocket.GetMediaSourceSettings(mediaItemName);
                followSoundSettings.Media.LocalFile = mediaSourcePath;
                obsService.OBSWebsocket.SetMediaSourceSettings(followSoundSettings);

                sceneItemProperties.Visible = true;
                obsService.OBSWebsocket.SetSceneItemProperties(sceneItemProperties, sceneName);

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            finally
            {
                obsService.OBSWebsocket.MediaEnded -= OBSWebsocket_MediaEnded;
            }
        }
    }
}
