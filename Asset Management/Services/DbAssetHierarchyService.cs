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
                root = new Asset { Id = "root", Name = "Root" };
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

        public bool AddNode(string parentId, Asset newNode)
        {
            var parent = _dbContext.Assets.FirstOrDefault(a => a.Id == parentId);
            if (parent == null) return false;

            // Prevent duplicate ID
            //if (_dbContext.Assets.Any(a => a.Id == newNode.Id)) return false;
            if(_dbContext.Assets.FirstOrDefault(a=> a.Id == newNode.Id)!=null){
                return false;
            }

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
        public bool RemoveNode(string nodeId)
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
            _dbContext.Assets.Remove(node);
        }


        public bool UpdateNode(string oldId, string newName)
        {
            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == oldId);
            bool checkName = _dbContext.Assets.Any(a => a.Name == newName);
            if (node == null || checkName == true) return false;

            string newId = GenerateIdFromName(newName);

            // Insert new node
            var newNode = new Asset
            {
                Id = newId,
                Name = newName,
                ParentId = node.ParentId
            };
            _dbContext.Assets.Add(newNode);
            _dbContext.SaveChanges();

            // Move children
            var children = _dbContext.Assets.Where(a => a.ParentId == oldId);
            foreach (var child in children)
            {
                child.ParentId = newId;
            }
            _dbContext.SaveChanges();

            // Delete old node
            _dbContext.Assets.Remove(node);
            _dbContext.SaveChanges();
            return true;


        }

        private string GenerateIdFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase, replace spaces with hyphens, remove invalid chars
            string id = name
                .ToLowerInvariant()
                .Trim()
                .Replace(" ", "-")  // Replace spaces with hyphens
                .Replace("\t", "-") // Replace tabs with hyphens
                .Replace("\n", "-") // Replace newlines with hyphens
                .Replace("\r", "-"); // Replace carriage returns with hyphens

            // Remove invalid characters (keep only a-z, 0-9, _, -)
            id = Regex.Replace(id, @"[^a-zA-Z0-9_-]", "");

            // Limit to 25 characters to leave room for timestamp suffix
            if (id.Length > 25)
                id = id.Substring(0, 25);

            // Add timestamp suffix to ensure uniqueness
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string timestampSuffix = (timestamp % 100000).ToString(); // Last 5 digits
            id = $"{id}_{timestampSuffix}";

            // Ensure it doesn't exceed 30 characters
            if (id.Length > 30)
                id = id.Substring(0, 30);

            return id;
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
        private Asset? FindNodeById(Asset node, string id)
        {
            if (node.Id.ToLower() == id.ToLower())
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
            // Check duplicate IDs
            bool hasDuplicateIds = _dbContext.Assets
                .GroupBy(a => a.Id.ToLower())
                .Any(g => g.Count() > 1);

            // Check duplicate Names
            bool hasDuplicateNames = _dbContext.Assets
                .GroupBy(a => a.Name.ToLower())
                .Any(g => g.Count() > 1);

            // Root special rule
            var root = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            if (root != null && (root.Id.ToLower() != "root" || root.Name.ToLower() != "root"))
            {
                return true;
            }

            return hasDuplicateIds || hasDuplicateNames;
        }
        public void ReplaceTree(Asset newRoot)
        {
            
            bool checkDuplicate = CheckDuplicated(newRoot);
            if (checkDuplicate)
                throw new Exception("Duplicate nodes present");

            var rootId = FindNodeById(newRoot, "root");
            var rootName = FindNodeByName(newRoot, "Root");
            //Console.WriteLine($"From Replace Tree method {rootId.Id} {rootName.Name}");
            

            // if root node is not present in the tree
            if (rootId == null && rootName == null)
            {

                Asset root = new Asset { Id = "root", Name = "Root", Children = new List<Asset> { newRoot } };
                // Clear DB
                _dbContext.Assets.RemoveRange(_dbContext.Assets);
                _dbContext.SaveChanges();

                _dbContext.ChangeTracker.Clear();

                _dbContext.Assets.Add(root);
                _dbContext.SaveChanges();
            }

            //first node is root 
            else if (newRoot.Id.ToLower() == "root" && newRoot.Name.ToLower() == "root")
            {
                // Clear DB
                _dbContext.Assets.RemoveRange(_dbContext.Assets);
                _dbContext.SaveChanges();

                _dbContext.ChangeTracker.Clear();

                _dbContext.Add(newRoot);
                _dbContext.SaveChanges();
            }

            else
            {

                //root present in the middle of the hierarchy tree
                throw new Exception("Root Id present in the middle of the hierarchy");
            }
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
            bool hasDuplicates = CheckDuplicated(newTree);
            if (hasDuplicates)
                throw new Exception("Duplicate nodes present in uploaded tree");

            // If uploaded tree itself is a root wrapper, skip it
            var nodesToMerge = newTree.Id == "root" && newTree.Name == "Root"
                ? newTree.Children
                : new List<Asset> { newTree };

            // Get the actual DB root
            var dbRoot = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            if (dbRoot == null)
            {
                // If DB is empty → insert new root
                dbRoot = new Asset { Id = "root", Name = "Root", Children = new List<Asset>() };
                _dbContext.Assets.Add(dbRoot);
                _dbContext.SaveChanges();
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
            // Step 1: Check if there is already a child with same Id or Name under currentParent
            var existingNode = _dbContext.Assets
                .FirstOrDefault(a => a.ParentId == currentParent.Id &&
                                     (a.Id == newNode.Id || a.Name == newNode.Name));

            if (existingNode != null)
            {
                int addedCount = 0;
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(existingNode, child);
                }
                return addedCount;
            }

            // Step 2: Check if node already exists anywhere in the tree (duplicate globally)
            var duplicateNode = _dbContext.Assets
                .FirstOrDefault(a => a.Id == newNode.Id || a.Name == newNode.Name);

            if (duplicateNode != null)
            {
                int addedCount = 0;
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(duplicateNode, child);
                }
                return addedCount;
            }

            // Step 3: No duplicates → add as a new child
            newNode.ParentId = currentParent.Id;   // attach to DB parent
            _dbContext.Assets.Add(newNode);

            return TreeLength(newNode);  // counts how many nodes were inserted under this branch
        }




    }
}
