using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    public partial class DiscordService
    {
        #region Private Properties

        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MessagingService MessagingService { get; }
        private EncryptionService EncryptionService { get; }
        private ModuleService ModuleService { get; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        #endregion

        #region Public Properties

        public DiscordSocketClient DiscordClient { get; }
        public InteractionService InteractionService { get; }
        public CommandService CommandService { get; }


        public event EventHandler<EventArgs>? ServiceStatusChanged;
        public bool? IsActive { get; set; } = false;

        #endregion

        #region Constructor

        public DiscordService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService, ModuleService moduleService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
            EncryptionService = encryptionService;
            ModuleService = moduleService;
            CancellationTokenSource = new();

            DiscordClient = new DiscordSocketClient();
            InteractionService = new(DiscordClient);
            CommandService = new();

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(DiscordService), typeof(DiscordService), this)
            });

            // TODO: Fix discord service initialization.
            ModuleService.InitializeSupportedEventsAndParameters(DiscordClient);
            ModuleService.InitializeSupportedEventsAndParameters(InteractionService);
            ModuleService.InitializeSupportedEventsAndParameters(CommandService);
        }

        #endregion

        #region Control Methods

        public Task StartAsync()
        {
            CancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                    string discordBotToken = await EncryptionService.Decrypt("DiscordBotToken", triggeredDbContext.Settings.GetSetting("DiscordBotToken"));
                    if (string.IsNullOrWhiteSpace(discordBotToken))
                    {
                        await MessagingService.AddMessage("The setting \"DiscordBotToken\" needs to be a valid bot token!", MessageCategory.Authentication, LogLevel.Error);
                        return;
                    }

                    ModuleService.RegisterEvents(DiscordClient);
                    ModuleService.RegisterEvents(InteractionService);
                    ModuleService.RegisterEvents(CommandService);

                    await MessagingService.AddMessage("Discord service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = null;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    DiscordClient.Connected += DiscordClient_Connected;
                    DiscordClient.Disconnected += DiscordClient_Disconnected;

                    _ = Task.Run(async () => await DiscordClient.LoginAsync(TokenType.Bot, discordBotToken));

                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    disconnections = 0;

                    ModuleService.DeregisterEvents(DiscordClient);
                    ModuleService.DeregisterEvents(InteractionService);
                    ModuleService.DeregisterEvents(CommandService);

                    await DiscordClient.LogoutAsync();

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await MessagingService.AddMessage("Discord service stopped!", MessageCategory.Service);
                }

            }, CancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async void DiscordClient_Connected(object? _, EventArgs? __)
        {
            disconnections = 0;
            await MessagingService.AddMessage("Discord service started!", MessageCategory.Service);
            IsActive = true;
            ServiceStatusChanged?.Invoke(this, new EventArgs());
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async void DiscordClient_Disconnected(object? _, Exception exception)
        {
            if (!CancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                CancellationTokenSource.Cancel();
                await MessagingService.AddMessage("Could not connect to Discord websocket after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!CancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await MessagingService.AddMessage($"Disconnected from Discord websocket: \"{exception.Message}\". Connection retrying...", MessageCategory.Service, LogLevel.Warning);


                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                string discordBotToken = await EncryptionService.Decrypt("DiscordBotToken", triggeredDbContext.Settings.GetSetting("DiscordBotToken"));
                _ = Task.Run(async () => await DiscordClient.LoginAsync(TokenType.Bot, discordBotToken));
            }

            lastDisconnection = DateTime.Now;
        }

        #endregion
    }
}
