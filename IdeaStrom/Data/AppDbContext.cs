using Microsoft.EntityFrameworkCore;
using IdeaStrom.Model;

namespace IdeaStrom.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 1. Register your existing models
        public DbSet<NotificationResponse> Notifications { get; set; }
        public DbSet<TopIdeaResponse> TopIdeas { get; set; }

        // 2. Add this line! This is what your Controller is looking for
        public DbSet<LoginResult> LoginResults { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 3. Tell EF these don't have a specific Primary Key table (since they come from SPs)
            modelBuilder.Entity<NotificationResponse>().HasNoKey();
            modelBuilder.Entity<TopIdeaResponse>().HasNoKey();
            modelBuilder.Entity<LoginResult>().HasNoKey();
            modelBuilder.Entity<AuditLog>().ToTable("t_AuditLogs","dbo"); // Force it to point to the plural table
            modelBuilder.Entity<AuditLog>().HasKey(x => x.Id);
        }
    }

    // 4. Define the class here if it's not in your Model folder
    public class LoginResult
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }
}