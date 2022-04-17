using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Triggered.Models
{
    public class TriggeredDbContext : DbContext
    {
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Vector> Vectors => Set<Vector>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Utility> Utilities => Set<Utility>();
        public DbSet<EventTest> EventTests => Set<EventTest>();
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
