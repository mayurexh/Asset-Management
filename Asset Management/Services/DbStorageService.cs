using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;

namespace Asset_Management.Services
{
    public class DbStorageService : IAssetStorageService
    {
        private readonly AssetDbContext _dbContext;
        public DbStorageService(AssetDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Asset LoadTree()
        {
            var root = _dbContext.Assets
                .FirstOrDefault(a => a.ParentId == null); // assuming root has no parent

            if (root == null)
            {
                return new Asset { Id = "root", Name = "Root" };
            }

            LoadChildren(root);
            return root;
        }

        private void LoadChildren(Asset parent)
        {
            _dbContext.Entry(parent)
                .Collection(p => p.Children)
                .Load();

            foreach (var child in parent.Children)
            {
                LoadChildren(child);
            }
        }


        public void SaveTree(Asset root)
        {
            _dbContext.Assets.Add(root);
            _dbContext.SaveChanges();
        }

        public string GetVersionedFileName()
        {
            return null;
        }

        public Asset ParseTree(string content)
        {
            return null;
        }



    }
}
