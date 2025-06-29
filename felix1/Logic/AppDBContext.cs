using Microsoft.EntityFrameworkCore;
using felix1.Logic;

namespace felix1.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<CashRegister> CashRegisters { get; set; } = null!;
        public DbSet<Table> Tables { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;


        private string _dbPath;

        //this is for storing de DB on a temporary location
        private string TempDBPath = "C:\\Users\\HP\\Desktop\\mita\\FELIX1PROY\\felix-1-proyect\\felix1\\tempDBStorage"; //for maria
        //private string TempDBPath = "C:\\Codes\\github\\felix-1-proyect\\felix1\\tempDBStorage";


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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Store enum as string
            modelBuilder.Entity<Article>()
                .Property(a => a.Category)
                .HasConversion<string>();

            // Self-referencing many-to-many for side dishes
            modelBuilder.Entity<Article>()
                .HasMany(a => a.SideDishes)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "ArticleSideDish",
                    j => j.HasOne<Article>().WithMany().HasForeignKey("SideDishId"),
                    j => j.HasOne<Article>().WithMany().HasForeignKey("ArticleId")
                );

            // FK for Cashier
            modelBuilder.Entity<CashRegister>()
                .HasOne(c => c.Cashier);


            // Define relationship between OrderItem and Article
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Article)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
                
            // Define relationship between Order and OrderItem
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // Define relationship between Order and User
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Waiter)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Order and Table
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Order and CashRegister
            modelBuilder.Entity<Order>()
                .HasOne(o => o.CashRegister)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
