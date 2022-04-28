using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service abstracted from <see cref="TwitchServiceBase"/> that handles connections to, registers events for and exposes <see cref="TwitchLib.Client.TwitchClient"/>.
    /// </summary>
    public class TwitchChatService : TwitchServiceBase
    {
        private MessagingService MessagingService { get; }
        private ModuleService ModuleService { get; }
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }

        /// <summary>
        /// Class offering easy way to interact with Twitch Chat. Please see the TwitchLib Client documentation here: https://swiftyspiffy.com/TwitchLib/Client/index.html
        /// </summary>
        public TwitchClient TwitchClient { get; set; } = new();


        /// <summary>
        /// Default constructor with injected services.
        /// </summary>       
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="moduleService">Injected <see cref="Services.ModuleService"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="encryptionService">Injected <see cref="EncryptionService"/>.</param>
        public TwitchChatService(IDbContextFactory<TriggeredDbContext> dbContextFactory,
                                ModuleService moduleService,
                                MessagingService messagingService,
                                EncryptionService encryptionService) 
            : base(dbContextFactory, messagingService, encryptionService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
            ModuleService = moduleService;

            ConnectFunction = Connect;
            DisconnectFunction = Disconnect;

            Scopes.AddRange(new AuthScopes[] 
            {
                AuthScopes.ChannelModerate,
                AuthScopes.ChatEdit,
                AuthScopes.ChatRead,
                AuthScopes.WhispersRead,
                AuthScopes.WhispersEdit 
            });

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(TwitchChatService), typeof(TwitchChatService), this),
            });
            ModuleService.InitializeSupportedEventsAndParameters(TwitchClient);
        }

        public override Task<bool> Initialize(string settingModifier = "Chat")
        {
            if (bool.TryParse(DbContextFactory.CreateDbContext().Settings.GetSetting("TwitchChatUseSecondAccount"), out bool useSecondAccount) && !useSecondAccount)
                settingModifier = string.Empty;

            return base.Initialize(settingModifier);
        }

        protected async Task Connect()
        {
            string? accessToken = await base.GetValidToken();
            if (accessToken == null)
            {
                await MessagingService.AddMessage("Could not start TwitchChat services! Access Token was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            TwitchClient.OnConnected += TwitchClient_OnConnected;
            TwitchClient.OnDisconnected += TwitchClient_OnDisconnected;
            TwitchClient.OnConnectionError += TwitchClient_OnConnectionError;
            TwitchClient.OnFailureToReceiveJoinConfirmation += TwitchClient_OnFailureToReceiveJoinConfirmation;

            TwitchClient.Initialize(new ConnectionCredentials(UserName, accessToken,  capabilities: new() { Commands = true, Membership = true, Tags = true}), ChannelName);
            await ModuleService.RegisterEvents(TwitchClient);
            TwitchClient.Connect();
        }

        protected async Task Disconnect()
        {
            await ModuleService.DeregisterEvents(TwitchClient);
            TwitchClient.Disconnect();
        }

        private async void TwitchClient_OnConnected(object? sender, OnConnectedArgs e)
        {
            await Task.Delay(5000);
            TwitchClient.JoinChannel(ChannelName);
            await MessagingService.AddMessage("TwitchChat connected!", MessageCategory.Service, LogLevel.Debug);
        }

        private async void TwitchClient_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            await Disconnected();
        }

        private async void TwitchClient_OnConnectionError(object? sender, OnConnectionErrorArgs e)
        {
            await Disconnected();
        }

        private async void TwitchClient_OnFailureToReceiveJoinConfirmation(object? sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            await MessagingService.AddMessage($"Could not connect to channel \"{e.Exception.Channel}\": {e.Exception.Details}", MessageCategory.Service, LogLevel.Error);
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async Task Disconnected()
        {
            if (!_cancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                _cancellationTokenSource.Cancel();
                await MessagingService.AddMessage("Could not connect to TwitchChat service after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!_cancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await MessagingService.AddMessage($"Disconnected from TwitchChat. Connection retrying...", MessageCategory.Service, LogLevel.Warning);

                TwitchClient.Connect();
            }

            lastDisconnection = DateTime.Now;
        }

    }
}
