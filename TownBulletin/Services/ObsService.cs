using Microsoft.EntityFrameworkCore;
using OBSWebsocketDotNet;
using TownBulletin.Extensions;
using TownBulletin.Models;

namespace TownBulletin.Services
{
    public partial class ObsService
    {
        #region Private Properties

        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;
        private readonly MessagingService _messagingService;
        private readonly EncryptionService _encryptionService;
        private readonly ModuleService _moduleService;

        private CancellationTokenSource _cancellationTokenSource;

        private string _obsHost = null!;
        private string _obsPassword = null!;

        #endregion

        #region Public Properties

        public OBSWebsocket OBSWebsocket { get; } = new();

        public event EventHandler<EventArgs>? ServiceStatusChanged;
        public bool IsActive { get; set; } = false;

        #endregion

        #region Constructor

        public ObsService(IDbContextFactory<TownBulletinDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService, ModuleService moduleService)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;
            _encryptionService = encryptionService;
            _moduleService = moduleService;
            _cancellationTokenSource = new();

            _moduleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(ObsService), typeof(ObsService), this)
            });

            _moduleService.InitializeSupportedEventsAndParameters(OBSWebsocket);
        }

        #endregion

        #region Control Methods

        public Task StartAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

                _obsHost = townBulletinDbContext.Settings.GetSetting("ObsAddress");
                if (string.IsNullOrWhiteSpace(_obsHost) || !_obsHost.StartsWith("ws://"))
                {
                    await _messagingService.AddMessage("The setting \"ObsHost\" host needs to be a valid websocket address!", MessageCategory.Service, LogLevel.Error);
                    return;
                }

                _obsPassword = await _encryptionService.Decrypt("ObsPassword", townBulletinDbContext.Settings.GetSetting("ObsPassword"));

                _moduleService.RegisterEvents(OBSWebsocket);

                try
                {
                    await _messagingService.AddMessage("OBS service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = true;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    OBSWebsocket.Connected += OBSWebsocket_Connected;
                    OBSWebsocket.Disconnected += OBSWebsocket_Disconnected;
                    OBSWebsocket.OBSExit += OBSWebsocket_OBSExit;

                    _ = Task.Run(() => OBSWebsocket.Connect(_obsHost, _obsPassword));

                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    disconnections = 0;
                    _moduleService.DeregisterEvents(OBSWebsocket);
                    OBSWebsocket.Disconnect();

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await _messagingService.AddMessage("OBS service stopped!", MessageCategory.Service);
                }

            }, _cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async void OBSWebsocket_Connected(object? sender, EventArgs e)
        {
            disconnections = 0;
            await _messagingService.AddMessage("OBS service started!.", MessageCategory.Service);
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async void OBSWebsocket_Disconnected(object? sender, EventArgs eventArgs)
        {
            if (!_cancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                _cancellationTokenSource.Cancel();
                await _messagingService.AddMessage("Could not connect to OBS websocket after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!_cancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await _messagingService.AddMessage($"Disconnected from OBS websocket. Connection retrying...", MessageCategory.Service, LogLevel.Warning);
                _ = Task.Run(() => OBSWebsocket.Connect(_obsHost, _obsPassword));
            }

            lastDisconnection = DateTime.Now;
        }

        private async void OBSWebsocket_OBSExit(object? sender, EventArgs eventArgs)
        {
            _cancellationTokenSource.Cancel();
            await _messagingService.AddMessage("OBS exited, service stopped.", MessageCategory.Service, LogLevel.Error);            
        }


        #endregion
    }
}
