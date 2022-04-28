using Microsoft.EntityFrameworkCore;

namespace Triggered.Models
{
    /// <summary>
    /// Context for the Triggered database.
    /// </summary>
    public class TriggeredDbContext : DbContext
    {
        /// <summary>
        /// A database set of <see cref="Setting"/>s.
        /// </summary>
        public DbSet<Setting> Settings => Set<Setting>();

        /// <summary>
        /// A database set of <see cref="Vector"/>s.
        /// </summary>
        public DbSet<Vector> Vectors => Set<Vector>();

        /// <summary>
        /// A database set of <see cref="Module"/>s.
        /// </summary>
        public DbSet<Module> Modules => Set<Module>();

        /// <summary>
        /// A database set of <see cref="Utility"/>s.
        /// </summary>
        public DbSet<Utility> Utilities => Set<Utility>();

        /// <summary>
        /// A database set of <see cref="EventTest"/>s.
        /// </summary>
        public DbSet<EventTest> EventTests => Set<EventTest>();

        /// <summary>
        /// A database set of <see cref="DataObject"/>s.
        /// </summary>
        public DbSet<DataObject> DataObjects => Set<DataObject>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dataPath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) ?? string.Empty, "data");
            Directory.CreateDirectory(dataPath);
            FileInfo databaseDestinationFileInfo = new(Path.Combine(dataPath, "triggered.db"));

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite($"Data Source=\"{databaseDestinationFileInfo.FullName}\";"); ;
        }        
    }
}
