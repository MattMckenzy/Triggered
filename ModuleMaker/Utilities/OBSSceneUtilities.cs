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
        /// <summary>
        /// Shows and plays a media item in given source, and hides the source one the media item is done playing.
        /// </summary>
        /// <param name="obsService">An instance of <see cref="ObsService"/>, used to control the given OBS scene.</param>
        /// <param name="mediaSourcePath">The path to the media item to play.</param>
        /// <param name="mediaItemName">The name of the source item used to play the media in OBS.</param>
        /// <param name="visibilityItemName">The name of the scene item that will be turned visible and subsequently hidden.</param>
        /// <param name="sceneName">The name of the scene that contains the media source item and the visibility scene item.</param>
        /// <param name="minimumSeconds">The minimum amount of time to show the scene. If the media is shorter, the scene will stay visible until this time is elapsed. Default is 5 seconds.</param>
        /// <param name="secondsToCancel">A timeout set, in case the media is too long or an issue occurs. Default is 5 minutes.</param>
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
