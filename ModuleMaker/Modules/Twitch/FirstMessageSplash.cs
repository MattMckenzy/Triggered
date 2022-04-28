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
    public class FirstMessageSplash
    {
        /// <summary>
        /// When a first-time chatter chat message is received, plays a random sound shown along with a random image with the "FolllowSplash" scene in OBS. Also sends first-time messages in Twitch Chat.
        /// </summary>
        public async static Task<bool> ShowFirstMessageHeffry(OnRitualNewChatterArgs eventArgs, QueueService queueService, ObsService obsService, TwitchService twitchService, TwitchChatService twitchChatService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
        {
            await queueService.Add("TopSplash", async () => {

                TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

                twitchChatService.TwitchClient.SendMessage(twitchService.ChannelName, $"Welcome to {twitchService.UserName}'s Channel, {eventArgs.RitualNewChatter.DisplayName}! Please be courteous and treat everyone with respect. However, feel free to interrupt him and ask him anything, he loves answering questions!");

                twitchChatService.TwitchClient.SendMessage(twitchService.ChannelName, $"Bienvenue à la chaîne de {twitchService.UserName}, {eventArgs.RitualNewChatter.DisplayName}! S'il vous plaît, restez poli et respectueux. Sentez-vous libre de l'interrompre avec tous vos questions, il adore jaser!");

                TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
                properties.Text = $"Welcome to the channel!\r\nBienvenue à la chaîne!\r\n{eventArgs.RitualNewChatter.DisplayName}";
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