using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Text.Json;

namespace Asset_Management.Services
{
    public class AssetHierarchyService : IAssetHierarchyService
    {
        private readonly IAssetStorageService _storage;
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

            parent.Children.Add(newNode);
            _storage.SaveTree(_root);
            return true;
        }

        public bool RemoveNode(string nodeId)
        {
            // Disallow deleting root
            if (_root.Id == nodeId)
                return false;

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
            _root = NewRoot;
            _storage.SaveTree(_root);
        }



    }
}
