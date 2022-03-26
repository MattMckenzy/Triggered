using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TownBulletin.Models;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TownBulletin.Services
{
    public class TwitchBotService : TwitchServiceBase
    {
        private readonly MessagingService _messagingService;
        private readonly ModuleService _moduleService;

        public TwitchClient TwitchClient { get; set; } = new();

        public TwitchBotService(IDbContextFactory<TownBulletinDbContext> dbContextFactory,
                                ModuleService moduleService,
                                MessagingService messagingService,
                                EncryptionService encryptionService) 
            : base(dbContextFactory, messagingService, encryptionService)
        {
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
                (nameof(TwitchBotService), typeof(TwitchBotService), this),
            });
            _moduleService.InitializeSupportedEventsAndParameters(TwitchClient);
        }

        public override Task<bool> Initialize(string settingModifier = "Bot")
        {
            return base.Initialize(settingModifier);
        }

        protected async Task Connect()
        {
            string? accessToken = await base.GetValidToken();
            if (accessToken == null)
            {
                await _messagingService.AddMessage("Could not start TwitchBot services! Access Token was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            TwitchClient.Initialize(new ConnectionCredentials(UserName, accessToken), ChannelName);
            _moduleService.RegisterEvents(TwitchClient);
            TwitchClient.Connect();
        }

        protected Task Disconnect()
        {
            _moduleService.DeregisterEvents(TwitchClient);
            TwitchClient.Disconnect();

            return Task.CompletedTask;
        }     
    }
}
