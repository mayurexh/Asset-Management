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


        }


    }
}
