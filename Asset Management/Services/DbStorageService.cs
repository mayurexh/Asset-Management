using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;

namespace Asset_Management.Services
{
    public class DbStorageService
    {
        private readonly AssetDbContext _dbContext;
        public DbStorageService(AssetDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        
    }
}
