using Microsoft.EntityFrameworkCore;
using Triggered.Models;

namespace Triggered.Services
{
    public partial class FileWatchingService
    {
        #region Private Properties

        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MessagingService MessagingService { get; }
        private ModuleService ModuleService { get; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        #endregion

        #region Public Properties

        public event EventHandler<EventArgs>? ServiceStatusChanged;

        public bool? IsActive { get; set; } = false;

        #endregion

        #region Constructor

        public FileWatchingService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, ModuleService moduleService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
            ModuleService = moduleService;
            CancellationTokenSource = new();

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(FileWatchingService), typeof(FileWatchingService), this)
            });

            ModuleService.InitializeSupportedEventsAndParameters(this);
        }

        #endregion

        #region Control Methods

        public async Task StartAsync()
        {
            CancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                    ModuleService.RegisterEvents(this);

                    await MessagingService.AddMessage("File watching service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = null;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    ModuleService.DeregisterEvents(this);

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await MessagingService.AddMessage("File watching service stopped!", MessageCategory.Service);
                }

            }, CancellationTokenSource.Token);

            return;
        }

        public Task StopAsync()
        {
            CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }   

        #endregion
    }
}
