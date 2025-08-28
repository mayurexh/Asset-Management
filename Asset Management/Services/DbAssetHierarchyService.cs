using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Asset_Management.Services
{
    
    public class DbAssetHierarchyService : IAssetHierarchyService
    {

        private readonly AssetDbContext _dbContext;

        public DbAssetHierarchyService(AssetDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public Asset GetHierarchy()
        {
            var root = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            if (root == null)
            {
                root = new Asset {Name = "Root"};
                _dbContext.Assets.Add(root);
                _dbContext.SaveChanges();
                return root;

            }
                

            LoadChildren(root);
            return root;
        }

        private void LoadChildren(Asset parent)
        {
            _dbContext.Entry(parent).Collection(p => p.Children).Load();
            foreach (var child in parent.Children)
            {
                LoadChildren(child);
            }
        }

        public bool AddNode(int parentId, Asset newNode)
        {
            var parent = _dbContext.Assets.FirstOrDefault(a => a.Id == parentId);
            Console.WriteLine($"FROM ADD NODE parentID = {parent.Id}, parentName = {parent.Name}");
            if (parent == null) return false;


            // Prevent duplicate Name 
            //if (parent.Children.Any(c => c.Name == newNode.Name)) return false;
            if (_dbContext.Assets.FirstOrDefault(a => a.Name == newNode.Name) != null)
            {

                return false;
            }


            parent.Children.Add(newNode);
            _dbContext.SaveChanges();
            return true;
        }

        public bool RemoveNode(int nodeId)
        {
            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == nodeId);
            if (node == null) return false;

            DeleteRecursively(node);   // handles children + node itself
            _dbContext.SaveChanges();
            return true;
        }
        private void DeleteRecursively(Asset node)
        {
            _dbContext.Entry(node).Collection(a => a.Children).Load();

            foreach (var child in node.Children.ToList())
            {
                DeleteRecursively(child);
            }

            // Mark node for deletion
            _dbContext.Assets.Remove(node);
        }


        public bool UpdateNode(int oldId, string newName)
        {
            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == oldId);
            if (node == null) return false;
            //check if same exists elsewhere
            var checkName = _dbContext.Assets.Any(a => a.Name == newName);

            if (!checkName)
            {
                node.Name = newName;
                _dbContext.SaveChanges();
                return true;
            }
            return false;



        }

        private Asset FindNodeByName(Asset node, string name)
        {

            if (node.Name.ToLower() == name.ToLower())
            {
                return node;

            }
            foreach (var child in node.Children)
            {

                var result = FindNodeByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;

        }
        private Asset? FindNodeById(Asset node, int id)
        {
            if (node.Id == id)
                return node;

            foreach (var child in node.Children)
            {
                var result = FindNodeById(child, id);
                if (result != null)
                    return result;
            }

            return null;
        }

        public int TotalAsset(Asset node)
        {
            return _dbContext.Assets.Count();
        }

        public bool CheckDuplicated(Asset node)
        {

            return true;
        }
        public void ReplaceTree(Asset newRoot)
        {

            // Check for duplicates in the incoming tree
            if (HasDuplicatesInTree(newRoot))
                throw new Exception("Duplicate nodes present in uploaded tree");

            // Check if incoming tree has a "Root" node
            if (ContainsRootNode(newRoot))
                throw new Exception("Node name \"Root\" present in the hierarchy.");

            try
            {
                // Try truncate table (faster, resets IDs)
                _dbContext.Database.ExecuteSqlRaw("TRUNCATE TABLE Assets");
            }
            catch
            {
                // Fallback: delete all one by one  + reseed identity
                _dbContext.Database.ExecuteSqlRaw("DELETE FROM Assets");
                _dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Assets', RESEED, 0)");
            }

            //    // Clear DB
            //    _dbContext.Assets.RemoveRange(_dbContext.Assets);
            //_dbContext.SaveChanges();
            //_dbContext.ChangeTracker.Clear();

            // Create new root
            var root = new Asset { Name = "Root" };
            _dbContext.Add(root);
            _dbContext.SaveChanges(); // Save to get the generated ID

            // Set parent relationships and add tree
            SetParentIds(newRoot, root.Id);
            _dbContext.Add(newRoot);
            _dbContext.SaveChanges();


        }

        private bool ContainsRootNode(Asset root)
        {
            if (root.Name.ToLower() == "root")
                return true;
            foreach(var child in root.Children)
            {
                ContainsRootNode(child);
            }
            return false;

        }
        private void SetParentIds(Asset node, int id)
        {
            node.ParentId = id;
            node.Id = 0;

            foreach (var child in node.Children)
            {
                SetParentIds(child, 0); // Will be updated after parent is saved
            }
        }
        private bool HasDuplicatesInTree(Asset root)
        {
            var names = new HashSet<string>();
            return CheckDuplicatesRecursively(root, names);
        }

        private bool CheckDuplicatesRecursively(Asset node, HashSet<string> names)
        {
            if (!names.Add(node.Name.ToLower()))
                return true; // Duplicate found

            foreach (var child in node.Children)
            {
                if (CheckDuplicatesRecursively(child, names))
                    return true;
            }
            return false;
        }


        public int TreeLength(Asset node)
        {
            int count = 1;

            _dbContext.Entry(node).Collection(n => n.Children).Load();
            foreach (var child in node.Children)
            {
                count += TreeLength(child);
            }

            return count;
        }

        public int MergeTree(Asset newTree)
        {
            int totalAdded = 0;

            // Global duplicate check in the incoming tree itself (before merge)
            bool hasDuplicates = HasDuplicatesInTree(newTree);
            if (hasDuplicates)
                throw new Exception("Duplicate nodes present in uploaded tree");

            //Since we're dealing with parsed trees, just use the tree as-is
            // No need to check for "root" wrapper since IDs are now auto-generated
            var nodesToMerge = new List<Asset> { newTree };

            // Get the actual DB root
            var dbRoot = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            if (dbRoot == null)
            {
                //Let EF Core generate the ID
                dbRoot = new Asset { Name = "Root", Children = new List<Asset>() };
                _dbContext.Assets.Add(dbRoot);
                _dbContext.SaveChanges(); // Save to get generated ID
            }

            foreach (var child in nodesToMerge)
            {
                totalAdded += MergeNode(dbRoot, child);
            }

            if (totalAdded > 0)
                _dbContext.SaveChanges();

            return totalAdded;
        }

        private int MergeNode(Asset currentParent, Asset newNode)
        {
            // ✅ Only check by Name since IDs will be auto-generated
            // Don't compare IDs from parsed files with DB IDs
            var existingNode = _dbContext.Assets
                .FirstOrDefault(a => a.ParentId == currentParent.Id &&
                                   a.Name.ToLower() == newNode.Name.ToLower());

            if (existingNode != null)
            {
                // Node exists under this parent → merge children
                int addedCount = 0;
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(existingNode, child);
                }
                return addedCount;
            }

            // ✅ Check globally by name only
            var duplicateNode = _dbContext.Assets
                .FirstOrDefault(a => a.Name.ToLower() == newNode.Name.ToLower());

            if (duplicateNode != null)
            {
                // Node exists elsewhere → merge children there
                int addedCount = 0;
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(duplicateNode, child);
                }
                return addedCount;
            }

            // ✅ No duplicates → add as new child
            newNode.Id = 0; // Reset to let EF Core generate new ID
            newNode.ParentId = currentParent.Id;

            // Reset all child IDs recursively
            //ResetChildIds(newNode);

            _dbContext.Assets.Add(newNode);
            return TreeLength(newNode);
        }

        //private void ResetChildIds(Asset node)
        //{
        //    foreach (var child in node.Children)
        //    {
        //        child.Id = 0;
        //        child.ParentId = 0; // Will be set when parent is saved
        //        //ResetChildIds(child);
        //    }
        //}




    }
}
