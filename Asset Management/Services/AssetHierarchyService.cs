using Asset_Management.Interfaces;
using Asset_Management.Models;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Asset_Management.Services
{
    public class AssetHierarchyService : IAssetHierarchyService
    {
        private readonly IAssetStorageService _storage;
        public static List<Asset> assetsAdded = new List<Asset>();
        private Asset _root;

        public AssetHierarchyService(IAssetStorageService storage)
        {
            _storage = storage;
            _root = _storage.LoadTree();
        }


        public Asset GetHierarchy()
        {
            return _root;
        }

        public async Task<bool> AddToRoot(string assetName)
        {
            return true;
        }

        public async Task<bool> AddNode(int parentId, Asset newNode)
        {
            var parent = FindNodeById(_root, parentId);
            if (parent == null)
                return false;


            // Prevent duplicated Asset Name
            if (FindNodeByName(_root, newNode.Name) != null)
                return false;
            

            parent.Children.Add(newNode);
            _storage.SaveTree(_root);
            return true;
        }

        public async Task<bool> RemoveNode(int nodeId)
        {
            // Disallow deleting root
            if (_root.Id == nodeId)
            {
                //_root = null;
                //_storage.DeleteTreeFile();
                return false;
            }

            bool removed = RemoveNodeRecursive(_root, nodeId);
            if (removed)
                _storage.SaveTree(_root);

            return removed;
        }

        private bool RemoveNodeRecursive(Asset current, int nodeId)
        {
            foreach (var child in current.Children.ToList())
            {
                if (child.Id == nodeId)
                {
                    current.Children.Remove(child);
                    return true;
                }

                if (RemoveNodeRecursive(child, nodeId))
                    return true;
            }
            return false;
        }
        public async Task<bool> UpdateNode(int oldId, string newName)
        {
            return false;
        }

        public async Task ReorderNode(int assetId, int parentId)
        {
            return;
        }
        private Asset FindNodeByName(Asset node, string name)
        {

            if (node.Name == name){
                return node;

            }
            foreach (var child in node.Children)
            { 

                var result = FindNodeByName(child, name);
                if(result != null)
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


        //Find total Asset 
        public int TotalAsset(Asset node)
        {
            int totalNode = 1;
            foreach(var child in node.Children)
            {
                totalNode += TotalAsset(child);
            }
            return totalNode;
        }

        //check for duplicate ids or Names
        public bool CheckDuplicated(Asset node)
        {
            HashSet<string> seenIds = new HashSet<string>();
            HashSet<string> seenNames = new HashSet<string>();

            return CheckDuplicatedRecursive(node, seenIds, seenNames);
        }

        private bool CheckDuplicatedRecursive(Asset node, HashSet<string> seenIds, HashSet<string> seenNames)
        {
            if (node == null) return false;

            // Convert to lowercase for case-insensitive comparison
            string nameLower = node.Name?.ToLowerInvariant();

            

            // Check duplicate Names
            if (!string.IsNullOrEmpty(nameLower))
            {
                if (seenNames.Contains(nameLower))
                    return true; // duplicate found
                seenNames.Add(nameLower);
            }

            

            // Recurse on children
            foreach (var child in node.Children)
            {
                if (CheckDuplicatedRecursive(child, seenIds, seenNames))
                    return true; // bubble up duplicate found
            }

            return false;
        }



        public void ReplaceTree(Asset NewRoot)
        {
            Console.WriteLine("FDS");

            
        }

        public int TreeLength(Asset node)
        {
            if (node == null)
                return 0;
            int totalNodes = 1;


            foreach (var child in node.Children)
            {
                totalNodes += TreeLength(child);
            }
            return totalNodes;
        }

        public int MergeTree(Asset newTree)
        {
            
            return 5;
        }

        private int MergeNode(Asset currentParent, Asset newNode)
        {
            // Step 1: Check if there is already a child with the same Id or Name under currentParent
            var existingNode = currentParent.Children
                .FirstOrDefault(c => c.Id == newNode.Id || c.Name == newNode.Name);

            if (existingNode != null)
            {
                int addedCount = 0;
                // Merge children recursively into this existing node
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(existingNode, child);
                }
                return addedCount;
            }

            // Step 2: Check if node already exists anywhere in the tree (duplicate Id or Name globally)
            var duplicateById = FindNodeById(_root, newNode.Id);
            var duplicateByName = FindNodeByName(_root, newNode.Name);

            if (duplicateById != null || duplicateByName != null)
            {
                // Found a global duplicate — merge children into that existing node
                var targetNode = duplicateById ?? duplicateByName;
                int addedCount = 0;
                foreach (var child in newNode.Children)
                {
                    addedCount += MergeNode(targetNode, child);
                }
                return addedCount;
            }

            // Step 3: No duplicates → add as a brand-new child
            currentParent.Children.Add(newNode);
            assetsAdded.Add(newNode);
            return TreeLength(newNode);
        }


    






    }
}
