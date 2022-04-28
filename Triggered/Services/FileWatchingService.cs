using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that registers <see cref="FileSystemWatcher"/>s for each path saved in the "FileWatcherPaths" <see cref="Setting"/> and registers all of their events for module execution.
    /// </summary>
    public class FileWatchingService
    {
        #region Private Properties

        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MessagingService MessagingService { get; }
        private ModuleService ModuleService { get; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private List<FileSystemWatcher> FileSystemWatchers { get; set; } = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Event handler that is invoked when the service is stopped, starting and started.
        /// </summary>
        public event EventHandler<EventArgs>? ServiceStatusChanged;

        /// <summary>
        /// Event that is invoked when any <see cref="FileSystemWatcher"/> notices a changed file or directory.
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileChanged;

        /// <summary>
        /// Event that is invoked when any <see cref="FileSystemWatcher"/> notices a created file or directory.
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileCreated;

        /// <summary>
        /// Event that is invoked when any <see cref="FileSystemWatcher"/> notices a deleted file or directory.
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileDeleted;

        /// <summary>
        /// Event that is invoked when any <see cref="FileSystemWatcher"/> notices a renamed file or directory.
        /// </summary>
        public event EventHandler<RenamedEventArgs>? FileRenamed;

        /// <summary>
        /// Returns true if the file watcher service has been started.
        /// </summary>
        public bool? IsActive { get; set; } = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="moduleService">Injected <see cref="Services.ModuleService"/>.</param>
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

                    foreach (string path in triggeredDbContext.Settings.GetSetting("FileWatcherPaths")
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        FileSystemWatcher fileSystemWatcher = new()
                        {
                            Path = path,
                            EnableRaisingEvents = true,
                            IncludeSubdirectories = true,
                            NotifyFilter = NotifyFilters.FileName |
                                NotifyFilters.DirectoryName |
                                NotifyFilters.Attributes |
                                NotifyFilters.Size |
                                NotifyFilters.LastWrite |
                                NotifyFilters.LastAccess |
                                NotifyFilters.CreationTime |
                                NotifyFilters.Security
                        };

                        fileSystemWatcher.Error += FileSystemWatcher_Error;
                        fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                        fileSystemWatcher.Created += FileSystemWatcher_Created;
                        fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
                        fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

                        FileSystemWatchers.Add(fileSystemWatcher);
                    }                    

                    await ModuleService.RegisterEvents(this);

                    await MessagingService.AddMessage("File watching service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = true;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    foreach (FileSystemWatcher fileSystemWatcher in FileSystemWatchers)
                        fileSystemWatcher.Dispose();

                    await ModuleService.DeregisterEvents(this);

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await MessagingService.AddMessage("File watching service stopped!", MessageCategory.Service);
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

        #endregion

        #region Event Handlers

        private async void FileSystemWatcher_Error(object sender, ErrorEventArgs eventArgs)
        {
            await MessagingService.AddMessage($"An error was encountered in the file watching service: {eventArgs.GetException().Message}");
            await StopAsync();
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs eventArgs)
            => FileChanged?.Invoke(sender, eventArgs);

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs eventArgs)
            => FileCreated?.Invoke(sender, eventArgs);

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs eventArgs)
            => FileDeleted?.Invoke(sender, eventArgs);

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs eventArgs)
            => FileRenamed?.Invoke(sender, eventArgs);

        #endregion
    }
}
