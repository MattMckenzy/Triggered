using Microsoft.EntityFrameworkCore;
using OBSWebsocketDotNet;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that connects to OBS with a configured address ("ObsAddress" <see cref="Setting"/>) and password ("ObsPassword" <see cref="Setting"/>), registers all OBS events and exposes a <see cref="OBSWebsocket"/> for further OBS interaction.
    /// </summary>
    public class ObsService
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
        /// Class offering ways to interact and control OBS through it's scene and source items and their properties. See this page for more information: https://github.com/BarRaider/obs-websocket-dotnet
        /// </summary>
        public OBSWebsocket OBSWebsocket { get; } = new();

        /// <summary>
        /// Event handler that is invoked when the service is stopped, starting and started.
        /// </summary>
        public event EventHandler<EventArgs>? ServiceStatusChanged;

        /// <summary>
        /// Returns true if the OBS service has been started.
        /// </summary>
        public bool? IsActive { get; set; } = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="moduleService">Injected <see cref="Services.ModuleService"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="encryptionService">Injected <see cref="EncryptionService"/>.</param>
        public ObsService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService, ModuleService moduleService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
            EncryptionService = encryptionService;
            ModuleService = moduleService;
            CancellationTokenSource = new();

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(ObsService), typeof(ObsService), this)
            });

            ModuleService.InitializeSupportedEventsAndParameters(OBSWebsocket);
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

                    string obsHost = triggeredDbContext.Settings.GetSetting("ObsAddress");
                    if (string.IsNullOrWhiteSpace(obsHost) || !obsHost.StartsWith("ws://"))
                    {
                        await MessagingService.AddMessage("The setting \"ObsHost\" host needs to be a valid websocket address!", MessageCategory.Service, LogLevel.Error);
                        return;
                    }

                    string obsPassword = await EncryptionService.Decrypt("ObsPassword", triggeredDbContext.Settings.GetSetting("ObsPassword"));

                    await ModuleService.RegisterEvents(OBSWebsocket);

                    await MessagingService.AddMessage("OBS service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = null;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    OBSWebsocket.Connected += OBSWebsocket_Connected;
                    OBSWebsocket.Disconnected += OBSWebsocket_Disconnected;
                    OBSWebsocket.OBSExit += OBSWebsocket_OBSExit;

                    _ = Task.Run(() => OBSWebsocket.Connect(obsHost, obsPassword));

                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    disconnections = 0;
                    await ModuleService.DeregisterEvents(OBSWebsocket);
                    OBSWebsocket.Disconnect();

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await MessagingService.AddMessage("OBS service stopped!", MessageCategory.Service);
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

        private async void OBSWebsocket_Connected(object? sender, EventArgs e)
        {
            disconnections = 0;
            await MessagingService.AddMessage("OBS service started!", MessageCategory.Service); 
            IsActive = true;
            ServiceStatusChanged?.Invoke(this, new EventArgs());
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async void OBSWebsocket_Disconnected(object? sender, EventArgs eventArgs)
        {
            if (!CancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                CancellationTokenSource.Cancel();
                await MessagingService.AddMessage("Could not connect to OBS websocket after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!CancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await MessagingService.AddMessage($"Disconnected from OBS websocket. Connection retrying...", MessageCategory.Service, LogLevel.Warning);


                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                string obsHost = triggeredDbContext.Settings.GetSetting("ObsAddress");
                string obsPassword = await EncryptionService.Decrypt("ObsPassword", triggeredDbContext.Settings.GetSetting("ObsPassword"));
                _ = Task.Run(() => OBSWebsocket.Connect(obsHost, obsPassword));
            }

            lastDisconnection = DateTime.Now;
        }

        private async void OBSWebsocket_OBSExit(object? sender, EventArgs eventArgs)
        {
            CancellationTokenSource.Cancel();
            await MessagingService.AddMessage("OBS exited, service stopped.", MessageCategory.Service, LogLevel.Error);            
        }


        #endregion
    }
}
