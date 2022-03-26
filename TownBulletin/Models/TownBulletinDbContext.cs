using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace TownBulletin.Models
{
    public class TownBulletinDbContext : DbContext
    {
        public TownBulletinDbContext(DbContextOptions<TownBulletinDbContext> options, IConfiguration configuration)
            : base(options)
        {
            ConnectionString = $"server={configuration["MySql:Host"]};user={configuration["MySql:User"]};password={configuration["MySql:Password"]};database={configuration["MySql:Database"]}";
        }

        public readonly string ConnectionString;

        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Vector> Vectors => Set<Vector>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<EventTest> EventTests => Set<EventTest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dataPath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) ?? string.Empty, "data");
            Directory.CreateDirectory(dataPath);
            FileInfo databaseDestinationFileInfo = new(Path.Combine(dataPath, "townbulletin.db"));

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite($"Data Source=\"{databaseDestinationFileInfo.FullName}\";"); ;
        }        
    }
}
