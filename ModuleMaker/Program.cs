#nullable enable
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine("Use this program to write and compile modules!");

OBSWebsocket OBSWebsocket = new();
OBSWebsocket.Connected += OBSWebsocket_Connected;
OBSWebsocket.Connect("ws://localhost:4444", "");

async void OBSWebsocket_Connected(object? sender, EventArgs e)
{
    int minimumSeconds = 5;
    DateTime minimumTime = DateTime.Now + TimeSpan.FromSeconds(minimumSeconds);

    SceneItemProperties sceneItemProperties = OBSWebsocket.GetSceneItemProperties("FollowSplash", "Animations");
    CancellationTokenSource cancellationTokenSource = new();
    cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

    async void OBSWebsocket_MediaEnded(object? sender, MediaEventArgs e)
    {
        cancellationTokenSource.Cancel();

        DateTime currentTime = DateTime.Now;
        if (currentTime < minimumTime)
            await Task.Delay((int)(minimumTime - currentTime).TotalMilliseconds);

        sceneItemProperties.Visible = false;
        OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animations");
    }

    try
    {
        OBSWebsocket.MediaEnded += OBSWebsocket_MediaEnded;

        TextGDIPlusProperties properties = OBSWebsocket.GetTextGDIPlusProperties("FollowText");
        properties.Text = $"Thank you for the follow!\r\nMerci beaucoup pour le suivi!\r\n";
        OBSWebsocket.SetTextGDIPlusProperties(properties);

        DirectoryInfo followResourcesDirectory = new("D:\\Streaming\\Animations\\Follow");

        Random random = new();
        FileInfo[] soundFiles = followResourcesDirectory.GetFiles("*.mp3", SearchOption.AllDirectories);
        FileInfo chosenSound = soundFiles[random.Next(soundFiles.Length)];
        FileInfo[] imageFiles = followResourcesDirectory.GetFiles("*.png", SearchOption.AllDirectories);
        FileInfo chosenImage = imageFiles[random.Next(imageFiles.Length)];

        SourceSettings mediaSourceSettings = OBSWebsocket.GetSourceSettings("FollowImage", "image_source");
        mediaSourceSettings.Settings["file"] = chosenImage.FullName;
        OBSWebsocket.SetSourceSettings("FollowImage", mediaSourceSettings.Settings);

        MediaSourceSettings followSoundSettings = OBSWebsocket.GetMediaSourceSettings("FollowSound");
        followSoundSettings.Media.LocalFile = chosenSound.FullName;
        OBSWebsocket.SetMediaSourceSettings(followSoundSettings);

        followSoundSettings = OBSWebsocket.GetMediaSourceSettings("FollowSound");

        sceneItemProperties.Visible = true;
        OBSWebsocket.SetSceneItemProperties(sceneItemProperties, "Animations");

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }
    }
    finally
    {
        OBSWebsocket.MediaEnded -= OBSWebsocket_MediaEnded;
    }
}

Console.WriteLine("Press any key to close.");
Console.ReadKey();