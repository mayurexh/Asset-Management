using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Asset_Management.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace Asset_Management.Services
{
    public enum AssetAction
    {
        AddAsset = 1,
        UpdateAsset = 2,
        DeleteAsset = 3,
        ReplaceHierarchy = 4,
        MergeHierarchy = 5
    }
    


    public class DbAssetHierarchyService : IAssetHierarchyService
    {

        private readonly AssetDbContext _dbContext;
        private readonly IAssetStorageService _storage;
        public static List<Asset> assetsAdded = new List<Asset>();
        public readonly IAssetLogService _logService;

        public DbAssetHierarchyService(AssetDbContext dbContext, IAssetStorageService storage, IAssetLogService logService)
        {
            _dbContext = dbContext;
            _storage = storage;
            _logService = logService;
        }

        private string SerializeJson(Asset asset)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };


            string json = JsonConvert.SerializeObject(asset, settings);

            return json;
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

        private void SaveHierarchyVersion(string? action = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                action = "None";

            //in memory objects reflect db state
            //saving in file for downloading and tracking purpose.
            //recursivley load children in root to represent deep hierarchy
            var root = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            LoadChildren(root);
            _storage.SaveTree(root, action);
        }
        private void LoadChildren(Asset parent)
        {
            _dbContext.Entry(parent).Collection(p => p.Children).Load();
            _dbContext.Entry(parent).Collection(p => p.Signals).Load();
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

            string action = "Asset Add";
            parent.Children.Add(newNode);
            _dbContext.SaveChanges();
            SaveHierarchyVersion(action);
            _logService.Log(action, asset: newNode.Name);

            return true;
        }
        public bool AddToRoot(string assetName)
        {
            bool isPresent = _dbContext.Assets.Any(a => a.Name == assetName);
            if (isPresent)
            {
                return false;
            }
            var root = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            var asset = new Asset
            {
                Name = assetName,
                Children = new List<Asset>(),
                Signals = new List<Signal>()

            };
            string action = "Asset Add";
            root.Children.Add(asset);
            _dbContext.SaveChanges();
            SaveHierarchyVersion(action);
            _logService.Log(action, assetName);
            return true;
        }

        public bool RemoveNode(int nodeId)
        {

            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == nodeId);
            if (node.ParentId == null) return false; //disallow deleting root node.
            if (node == null) return false;

            DeleteRecursively(node);   // handles children + node itself
            _dbContext.SaveChanges();
            string action = "Delete Asset";
            SaveHierarchyVersion(action);
            _logService.Log(action, node.Name);
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
            if (node.ParentId == null) return false; //disallow updating root node.
            if (node == null) return false;
            //check if same exists elsewhere
            var checkName = _dbContext.Assets.Any(a => a.Name == newName);

            if (!checkName)
            {

                node.Name = newName;
                _dbContext.SaveChanges();

                string action = "Update Asset";
                SaveHierarchyVersion(action);
                _logService.Log(action, newName);
                
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

            try
            {
                // Create new root
                string action = "Replace Hierarchy";
                var json = SerializeJson(newRoot); //json before saving to database for log purposes
                var root = new Asset { Name = "Root" };
                _dbContext.Add(root);
                _dbContext.SaveChanges(); // Save to get the generated ID


                // Set parent relationships and add tree
                ResetIds(newRoot);
                SetParentIds(newRoot, root.Id);
                _dbContext.Add(newRoot);
                _dbContext.SaveChanges();
                _logService.Log(action, asset: json);
                SaveHierarchyVersion(action);
            }catch(DbUpdateException ex)
            {

                throw;
            }
            


        }
        private void ResetIds(Asset asset)
        {
            asset.Id = 0;
            foreach (var s in asset.Signals)
                s.Id = 0;

            foreach (var child in asset.Children)
                ResetIds(child);
        }

        private bool ContainsRootNode(Asset root)
        {
            if (root.Name.ToLower() == "root")
                return true;
            foreach(var child in root.Children)
            {
                if (ContainsRootNode(child))
                    return true;
            }
            return false;

        }
        private void SetParentIds(Asset node, int id)
        {
            node.ParentId = id;
            node.Id = 0;

            foreach (var child in node.Children)
            {
                child.Id = 0;
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
            string action = "Merge Hierarchy";
            var json = SerializeJson(newTree);
            // Global duplicate check in the incoming tree itself (before merge)
            bool hasDuplicates = HasDuplicatesInTree(newTree);
            if (hasDuplicates)
                throw new Exception("Duplicate nodes present in uploaded tree");

            // Get the actual DB root
            var dbRoot = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            if (dbRoot == null)
            {
                dbRoot = new Asset { Name = "Root" };
                _dbContext.Assets.Add(dbRoot);
                _dbContext.SaveChanges(); // Save to get generated ID
            }
            foreach(var child in newTree.Children)
            {
                totalAdded += MergeNode(dbRoot, child);

            }


            if (totalAdded > 0)
                _dbContext.SaveChanges();

            //recursivley load children in root to represent deep hierarchy
            var dbroot = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            LoadChildren(dbroot);
            _storage.SaveTree(dbroot);
            _logService.Log(action, asset: json);
            SaveHierarchyVersion(action);


            return totalAdded;
        }

        private int MergeNode(Asset currentParent, Asset newNode)
        {
            // FIRST: Check if node exists GLOBALLY by name (most important check)
            var globalMatch = _dbContext.Assets.Include(a=>a.Signals)
                .FirstOrDefault(a => a.Name.ToLower() == newNode.Name.ToLower());

            if (globalMatch != null)
            {
                // Node exists somewhere - merge all children into it
                int addedCount = 0;

                //if (newNode.Signals != null && newNode.Signals.Any())
                //{
                //    foreach (var signal in newNode.Signals)
                //    {
                //        // Check if signal already exists under this asset
                //        bool exists = _dbContext.Signals.Any(s =>
                //            s.Name.ToLower() == signal.Name.ToLower() &&
                //            s.AssetId == globalMatch.Id);

                //        if (!exists)
                //        {
                //            signal.Id = 0; // let EF assign
                //            signal.AssetId = globalMatch.Id;
                //            _dbContext.Signals.Add(signal);
                //        }
                //    }
                //}


                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(globalMatch, child);
                }
                return addedCount;
            }

            // SECOND: If no global match, add as new node under current parent
            newNode.Id = 0; // Let EF generate new ID
            newNode.ParentId = currentParent.Id;

            // Reset all child IDs recursively
            ResetChildIds(newNode);

            _dbContext.Assets.Add(newNode);

            assetsAdded.Add(newNode);

            return TreeLength(newNode);
        }

        private void ResetChildIds(Asset node)
        {
            foreach (var child in node.Children)
            {
                child.Id = 0;
                child.ParentId = 0; // Will be set by EF Core navigation properties
                ResetChildIds(child);
            }
        }




    }
}
