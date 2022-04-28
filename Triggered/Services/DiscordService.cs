using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;
using static Discord.WebSocket.DiscordSocketClient;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that connects to Discord with a configured bot access token ("DiscordBotToken" <see cref="Setting"/>), registers all Discord events and exposes a <see cref="Discord.WebSocket.DiscordSocketClient"/> for further Discord interaction.
    /// </summary>
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

        /// <summary>
        /// Class offering methods to interact with Discord guilds, channels, users and more, by means of an authenticated bot token. View the page here for more information: https://discordnet.dev/api/Discord.WebSocket.DiscordSocketClient.html
        /// </summary>
        public DiscordSocketClient DiscordSocketClient { get; }

        /// <summary>
        /// Event handler that is invoked when the service is stopped, starting and started.
        /// </summary>
        public event EventHandler<EventArgs>? ServiceStatusChanged;

        /// <summary>
        /// Returns true if the Discord Service has been started.
        /// </summary>
        public bool? IsActive { get; set; } = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="encryptionService">Injected <see cref="Services.EncryptionService"/>.</param>
        /// <param name="moduleService">Injected <see cref="Services.ModuleService"/>.</param>
        public DiscordService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService, ModuleService moduleService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
            EncryptionService = encryptionService;
            ModuleService = moduleService;
            CancellationTokenSource = new();

            DiscordSocketClient = new DiscordSocketClient();

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(DiscordService), typeof(DiscordService), this)
            });

            ModuleService.InitializeSupportedEventsAndParameters(DiscordSocketClient);
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Starts the service.
        /// </summary>
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

                    await ModuleService.RegisterEvents(DiscordSocketClient);

                    await MessagingService.AddMessage("Discord service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = null;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    DiscordSocketClient.Connected += DiscordClient_Connected;
                    DiscordSocketClient.Disconnected += DiscordClient_Disconnected;

                    await DiscordSocketClient.LoginAsync(TokenType.Bot, discordBotToken);
                    await DiscordSocketClient.StartAsync();

                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    disconnections = 0;

                    await ModuleService.DeregisterEvents(DiscordSocketClient);

                    await DiscordSocketClient.LogoutAsync();

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await MessagingService.AddMessage("Discord service stopped!", MessageCategory.Service);
                }

            }, CancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
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
        private async void DiscordClient_Disconnected(object? _, DisconnectedArguments arguments)
        {
            if (!CancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                CancellationTokenSource.Cancel();
                await MessagingService.AddMessage("Could not connect to Discord websocket after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!CancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await MessagingService.AddMessage($"Disconnected from Discord websocket: \"{arguments.Exception.Message}\". Connection retrying...", MessageCategory.Service, LogLevel.Warning);


                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                string discordBotToken = await EncryptionService.Decrypt("DiscordBotToken", triggeredDbContext.Settings.GetSetting("DiscordBotToken"));
                _ = Task.Run(async () => await DiscordSocketClient.LoginAsync(TokenType.Bot, discordBotToken));
            }

            lastDisconnection = DateTime.Now;
        }

        #endregion
    }
}
