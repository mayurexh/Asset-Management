using Asset_Management.Models;
using System.Text.Json;

namespace Asset_Management.Services
{
    public class AssetHierarchyService
    {
        private readonly string _dataFile;
        private Asset _root;

        public AssetHierarchyService(IWebHostEnvironment env)
        {
            _dataFile = Path.Combine(env.ContentRootPath, "assets.json");
            LoadTree();
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

            if (FindNodeById(_root, newNode.Id) != null)
                return false;

            parent.Children.Add(newNode);
            SaveTree();
            return true;
        }

        public bool RemoveNode(string nodeId)
        {
            if (_root.Id == nodeId)
                return false;

            bool removed = RemoveNodeRecursive(_root, nodeId);
            if (removed)
                SaveTree();

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

        private void LoadTree()

        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            if (File.Exists(_dataFile))
            {
                string json = File.ReadAllText(_dataFile);
                _root = JsonSerializer.Deserialize<Asset>(json,options) ?? new Asset { Id = "root", Name = "Root" };
                
            }
            else
            {
                _root = new Asset { Id = "root", Name = "Root" };
                SaveTree();
            }
            Console.WriteLine($"Root ID: {_root?.Id}, Name: {_root?.Name}");

        }

        private void SaveTree()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_root, options);
            File.WriteAllText(_dataFile, json);
        }
    }
}
