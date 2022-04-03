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
    public class TwitchChatService : TwitchServiceBase
    {
        private readonly MessagingService _messagingService;
        private readonly ModuleService _moduleService;
        private readonly IDbContextFactory<TriggeredDbContext> _dbContextFactory;

        public TwitchClient TwitchClient { get; set; } = new();


        public TwitchChatService(IDbContextFactory<TriggeredDbContext> dbContextFactory,
                                ModuleService moduleService,
                                MessagingService messagingService,
                                EncryptionService encryptionService) 
            : base(dbContextFactory, messagingService, encryptionService)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;
            _moduleService = moduleService;

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

            _moduleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(TwitchChatService), typeof(TwitchChatService), this),
            });
            _moduleService.InitializeSupportedEventsAndParameters(TwitchClient);
        }

        public override Task<bool> Initialize(string settingModifier = "Chat")
        {
            _ = bool.TryParse(_dbContextFactory.CreateDbContext().Settings.GetSetting("TwitchChatUseSecondAccount"), out bool useSecondAccount);
            if (!useSecondAccount)
                settingModifier = string.Empty;

            return base.Initialize(settingModifier);
        }

        protected async Task Connect()
        {
            string? accessToken = await base.GetValidToken();
            if (accessToken == null)
            {
                await _messagingService.AddMessage("Could not start TwitchChat services! Access Token was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            TwitchClient.OnConnected += TwitchClient_OnConnected;
            TwitchClient.OnDisconnected += TwitchClient_OnDisconnected;
            TwitchClient.OnConnectionError += TwitchClient_OnConnectionError;
            TwitchClient.OnFailureToReceiveJoinConfirmation += TwitchClient_OnFailureToReceiveJoinConfirmation;

            TwitchClient.Initialize(new ConnectionCredentials(UserName, accessToken,  capabilities: new() { Commands = true, Membership = true, Tags = true}), ChannelName);
            _moduleService.RegisterEvents(TwitchClient);
            TwitchClient.Connect();
        }

        protected Task Disconnect()
        {
            _moduleService.DeregisterEvents(TwitchClient);
            TwitchClient.Disconnect();

            return Task.CompletedTask;
        }

        private async void TwitchClient_OnConnected(object? sender, OnConnectedArgs e)
        {
            await Task.Delay(5000);
            TwitchClient.JoinChannel(ChannelName);
            await _messagingService.AddMessage("TwitchChat connected!", MessageCategory.Service, LogLevel.Debug);
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
            await _messagingService.AddMessage($"Could not connect to channel \"{e.Exception.Channel}\": {e.Exception.Details}", MessageCategory.Service, LogLevel.Error);
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async Task Disconnected()
        {
            if (!_cancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                _cancellationTokenSource.Cancel();
                await _messagingService.AddMessage("Could not connect to TwitchChat service after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!_cancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await _messagingService.AddMessage($"Disconnected from TwitchChat. Connection retrying...", MessageCategory.Service, LogLevel.Warning);

                TwitchClient.Connect();
            }

            lastDisconnection = DateTime.Now;
        }

    }
}
