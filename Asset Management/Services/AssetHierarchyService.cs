using Asset_Management.Interfaces;
using Asset_Management.Models;
using Newtonsoft.Json;
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

        public void ReplaceTree(Asset NewRoot)
        {
            //Deserialize the json data using NewtonsoftJson
            _root = NewRoot;
            _storage.SaveTree(_root);

            
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

    foreach (var child in newTree.Children)
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
    // Try to find matching node under currentParent
    var existingNode = currentParent.Children
        .FirstOrDefault(c => c.Id == newNode.Id || c.Name == newNode.Name);

    if (existingNode != null)
    {
        int addedCount = 0;
        // Merge children recursively
        foreach (var child in newNode.Children)
        {
            addedCount += MergeNode(existingNode, child);
        }
        return addedCount;
    }
    else
    {
        // No match found → Add as new child
        currentParent.Children.Add(newNode);
        assetsAdded.Add(newNode);
        return TreeLength(newNode);
    }
}




    }
}
