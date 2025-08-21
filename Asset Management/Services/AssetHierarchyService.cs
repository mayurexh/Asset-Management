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

        public bool AddNode(string parentId, Asset newNode)
        {
            var parent = FindNodeById(_root, parentId);
            if (parent == null)
                return false;

            // Prevent duplicate ID
            if (FindNodeById(_root, newNode.Id) != null)
                return false;

            // Prevent duplicated Asset Name
            if (FindNodeByName(_root, newNode.Name) != null)
                return false;
            

            parent.Children.Add(newNode);
            _storage.SaveTree(_root);
            return true;
        }

        public bool RemoveNode(string nodeId)
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

        private bool RemoveNodeRecursive(Asset current, string nodeId)
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
        private Asset? FindNodeById(Asset node, string id)
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
            string idLower = node.Id?.ToLowerInvariant();
            string nameLower = node.Name?.ToLowerInvariant();

            // Check duplicate IDs
            if (!string.IsNullOrEmpty(idLower))
            {
                if (seenIds.Contains(idLower))
                    return true; // duplicate found
                seenIds.Add(idLower);
            }

            // Check duplicate Names
            if (!string.IsNullOrEmpty(nameLower))
            {
                if (seenNames.Contains(nameLower))
                    return true; // duplicate found
                seenNames.Add(nameLower);
            }

            // Special rule for root: id and name must both be "root"
            if ((idLower == "root" && nameLower != "root") ||
                (idLower != "root" && nameLower == "root"))
            {
                return true; // invalid root
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
            //check root node is present in the tree anywhere
            var rootIdPresent = FindNodeById(NewRoot, "root");
            var rootNamePresent = FindNodeByName(NewRoot, "Root");


            bool checkDuplicate = CheckDuplicated(NewRoot);
            if (checkDuplicate)
            {
                throw new Exception("Duplicate nodes present");
            }

            // if root node is not present in the tree
            if(rootIdPresent == null && rootNamePresent == null)
            {
                //wrap uploaded tree within a root node
                Asset root = new Asset { Id = "root", Name = "Root", Children = new List<Asset> { NewRoot } };
                _root = root;
                _storage.SaveTree(_root);
            }

            // first node is the root node
            else if(NewRoot.Id == "root" && NewRoot.Name == "Root") 
            {
                _root = NewRoot;
                _storage.SaveTree(_root);
            }
            else
            {
                //root present in the middle of the hierarchy tree
                throw new Exception("Root Id present in the middle of the hierarchy");

            }


            
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
            int totalAdded = 0;
            bool hasDuplicates = CheckDuplicated(newTree);
            if (hasDuplicates)
            {
                throw new Exception("Duplicate nodes present");
            }


            // If uploaded tree itself is a root wrapper, skip it
            var nodesToMerge = newTree.Id == "root" && newTree.Name == "Root"
                ? newTree.Children
                : new List<Asset> { newTree};

            

            foreach (var child in nodesToMerge)
            {
                totalAdded += MergeNode(_root, child);
            }

            if (totalAdded > 0)
            {
                _storage.SaveTree(_root);
            }

            return totalAdded;
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
