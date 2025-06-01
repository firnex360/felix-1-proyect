using Microsoft.EntityFrameworkCore;
using felix1.Logic;

namespace felix1.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        private string _dbPath;

        //this is for storing de DB on a temporary location
        private string TempDBPath = "C:\\Codes\\github\\felix-1-proyect\\felix1\\tempDBStorage";

        public AppDbContext()
        {
            //on production the lines of code below should be uncommented
            //var folder = FileSystem.AppDataDirectory;
            //_dbPath = Path.Combine(folder, "appdata.db");

            _dbPath = Path.Combine(TempDBPath, "appdata.db");
            Database.EnsureCreated(); // creates DB if not exists
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={_dbPath}");
        }
    }
}
