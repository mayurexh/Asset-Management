using Asset_Management.Database;
using Asset_Management.DTO;
using Asset_Management.Hubs;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Asset_Management.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Security.Claims;
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
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public DbAssetHierarchyService(AssetDbContext dbContext, IAssetStorageService storage, IAssetLogService logService, IHubContext<NotificationHub> hubContext, IHttpContextAccessor httpContextAccessor, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _storage = storage;
            _logService = logService;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private string? GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        private string? GetCurrentUserID()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


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

        public async Task<bool> AddNode(int parentId, Asset newNode)
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

            var currentUserId = GetCurrentUserID();
            List<string> connectionIds = NotificationHub.GetConnections(currentUserId);
            await _hubContext.Clients.AllExcept(connectionIds).SendAsync("RecieveAssetNotification", new
            {
                Type = "AssetAdded",
                User = GetCurrentUser(),
                Name = $"{newNode.Name}"
            });




            return true;
        }
        public async Task ReorderNode(int assetId, int parentId)
        {
            try
            {
                var parent = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == parentId);
                var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId);

                if (parent == null)
                    throw new Exception("Target parent asset not found");
                if (asset == null)
                    throw new Exception("Asset to move not found");

                // Check if trying to move asset under itself
                if (assetId == parentId)
                    throw new Exception("Cannot move asset under itself");

                // Check if trying to move asset under its descendant (would create circular reference)
                // We need to check if the NEW PARENT is a descendant of the ASSET BEING MOVED
                if (await IsDescendant(assetId, parentId))
                    throw new Exception("Cannot move asset under its descendant - this would create a circular reference");

                // Check if asset is already under this parent
                if (asset.ParentId == parentId)
                    throw new Exception("Asset is already under the specified parent");

                asset.ParentId = parentId;
                asset.Parent = parent;
                await _dbContext.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("Invalid asset operation");
            }
        }

        // Helper method to check if targetId is a descendant of ancestorId
        private async Task<bool> IsDescendant(int ancestorId, int targetId)
        {
            var ancestorAsset = await _dbContext.Assets
                .Include(a => a.Children)
                .FirstOrDefaultAsync(a => a.Id == ancestorId);

            if (ancestorAsset == null) return false;

            return await CheckDescendantRecursive(ancestorAsset, targetId);
        }

        private async Task<bool> CheckDescendantRecursive(Asset currentAsset, int targetId)
        {
            if (currentAsset.Children == null || !currentAsset.Children.Any())
                return false;

            foreach (var child in currentAsset.Children)
            {
                if (child.Id == targetId)
                    return true;

                // Load children for recursive check
                var childWithChildren = await _dbContext.Assets
                    .Include(a => a.Children)
                    .FirstOrDefaultAsync(a => a.Id == child.Id);

                if (childWithChildren != null && await CheckDescendantRecursive(childWithChildren, targetId))
                    return true;
            }

            return false;
        }
        public async Task<bool> AddToRoot(string assetName)
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


            //signal r
            var currentUserId = GetCurrentUserID();
            var username = GetCurrentUser();
            List<string> connectionIds = NotificationHub.GetConnections(currentUserId);

            await _hubContext.Clients.GroupExcept("Role_Admin", connectionIds).SendAsync("RecieveAssetNotification", new
            {
                Type = "AssetAdded",
                User = username,
                Name = assetName
            });

            await _hubContext.Clients.Group("Role_Viewer").SendAsync("RecieveAssetNotification", new
            {
                Type = "AssetAdded",
                User = "Admin",
                Name = assetName
            });

            return true;
        }   

        public async Task<bool> RemoveNode(int nodeId)
        {

            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == nodeId);
            if (node.ParentId == null) return false; //disallow deleting root node.
            if (node == null) return false;

            string name = node.Name; //for notification purpose
            DeleteRecursively(node);   // handles children + node itself
            _dbContext.SaveChanges();
            string action = "Delete Asset";
            SaveHierarchyVersion(action);
            _logService.Log(action, node.Name);

            var currentUserId = GetCurrentUserID();
            List<string> connectionIds = NotificationHub.GetConnections(currentUserId);

            
            await _hubContext.Clients.GroupExcept("Role_Admin", connectionIds).SendAsync(
                "RecieveAssetNotification", new
                {
                    Type = "AssetDeleted",
                    User = GetCurrentUser(),
                    Name = $"{name}"
                }
                );

            await _hubContext.Clients.Group("Role_Viewer").SendAsync(
                "RecieveAssetNotification", new
                {
                    Type = "AssetDeleted",
                    User = "Admin",
                    Name = $"{name}"
                }
                );


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


        public async Task<bool> UpdateNode(int oldId, string newName)
        {
            var node = _dbContext.Assets.FirstOrDefault(a => a.Id == oldId);
            if (node.ParentId == null) return false; //disallow updating root node.
            if (node == null) return false;
            //check if same exists elsewhere
            var checkName = _dbContext.Assets.Any(a => a.Name == newName);

            if (!checkName)
            {
                string oldName = node.Name; //for notification purpose 

                node.Name = newName;
                _dbContext.SaveChanges();

                string action = "Update Asset";
                SaveHierarchyVersion(action);
                _logService.Log(action, newName);
                var currentUserId = GetCurrentUserID();
                List<string> connectionIds = NotificationHub.GetConnections(currentUserId);

                await _hubContext.Clients.GroupExcept("Role_Admin", connectionIds).SendAsync(
                    "RecieveAssetNotification", new
                    {
                        Type = "AssetUpdated",
                        User = GetCurrentUser(),
                        OldName = $"{oldName}",
                        NewName = $"{newName}"
                    }
                );

                await _hubContext.Clients.Group("Role_Viewer").SendAsync(
                    "RecieveAssetNotification", new
                    {
                        Type = "AssetUpdated",
                        User = "Admin",
                        OldName = $"{oldName}",
                        NewName = $"{newName}"
                    }
                );



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
