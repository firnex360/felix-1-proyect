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
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<Refund> Refunds { get; set; } = null!;

        private string _dbPath;


        //this is for storing de DB on a temporary location
        private string TempDBPath = "C:\\Codes\\github\\felix-1-proyect\\felix1\\tempDBStorage";
        //private string TempDBPath = "C:\\Users\\HP\\Desktop\\mita\\FELIX1PROY\\felix-1-proyect\\felix1\\tempDBStorage"; //for maria
        //private string TempDBPath = "C:\\Users\\dell\\Source\\Repos\\felix-1-proyect\\felix1\\tempDBStorage\\"; //ChenFan

        public AppDbContext()
        {
            _dbPath = Path.Combine(TempDBPath, "appdata.db");
            Database.EnsureCreated();
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

            // Define relationship between Transaction and Order
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Transaction and Refund
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Refund)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Refund and Order
            modelBuilder.Entity<Refund>()
                .HasOne(r => r.Order)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Refund and User
            modelBuilder.Entity<Refund>()
                .HasOne(r => r.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship between Refund and OrderItem
            modelBuilder.Entity<Refund>()
                .HasMany(r => r.RefundedItems)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }

        public static async Task ExecuteSafeAsync(Func<AppDbContext, Task> operation, Action<Exception> errorHandler = null!)
        {
            try
            {
                using var db = new AppDbContext();
                await operation(db);
            }
            catch (DbUpdateException dbEx)
            {
                errorHandler?.Invoke(dbEx);
                Console.WriteLine($"Database update error: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
                Console.WriteLine($"General error: {ex.Message}");
                throw;
            }
        }

        public static async Task<T> ExecuteSafeAsync<T>(Func<AppDbContext, Task<T>> operation, Action<Exception> errorHandler = null!)
        {
            try
            {
                using var db = new AppDbContext();
                return await operation(db);
            }
            catch (DbUpdateException dbEx)
            {
                errorHandler?.Invoke(dbEx);
                Console.WriteLine($"Database update error: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
                Console.WriteLine($"General error: {ex.Message}");
                throw;
            }
        }
    }
}