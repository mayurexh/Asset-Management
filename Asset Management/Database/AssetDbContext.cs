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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure self-referencing relationship
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Parent)          // each asset has one parent
                .WithMany(a => a.Children)      // parent can have many children
                .HasForeignKey(a => a.ParentId) // FK is ParentId
                .OnDelete(DeleteBehavior.ClientCascade); // prevent cascade delete from nuking whole tree
        }


    }
}
