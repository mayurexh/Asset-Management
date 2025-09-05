using Asset_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management.Database
{
    public class AssetDbContext : DbContext
    {
        public AssetDbContext(DbContextOptions<AssetDbContext> options) : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Signal> Signals { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<HierarchyVersion> HierarchyVersions { get; set; }

        public DbSet<AssetLog> AssetLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure self-referencing relationship
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Parent)          // each asset has one parent
                .WithMany(a => a.Children)      // parent can have many children
                .HasForeignKey(a => a.ParentId) // FK is ParentId
                .OnDelete(DeleteBehavior.ClientCascade); // prevent cascade delete from nuking whole tree
                                                         // Configure Asset → Signal relationship explicitly
            modelBuilder.Entity<Signal>()
                .HasOne(s => s.Asset)            // Signal belongs to one Asset
                .WithMany(a => a.Signals)        // Asset has many Signals
                .HasForeignKey(s => s.AssetId)   // FK is AssetId
                .OnDelete(DeleteBehavior.Cascade); // delete signals when asset is deleted

            // Unique constraint - Signal names must be unique within same Asset
            // Handle using DbUpdateException
            modelBuilder.Entity<Signal>()
                .HasIndex(s => new { s.AssetId, s.Name })
                .IsUnique();

            // unique username constrain for Users
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // seed admin
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                PasswordHash = "AQAAAAIAAYagAAAAEIdgTPG+a4yi3RXmgJdIP+/vlZvopEnVlHnjJDJmVq/rGjZljrxpK1RoZ+iFAg0mhw==",
                CreatedAtUtc = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            });

            
        }


    }
}
