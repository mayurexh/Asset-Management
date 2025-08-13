using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Text.Json;

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


        public int MergeTree(Asset NewAdditonTree)
        { 
            bool allGood = true;

            //track total valid added nodes
            int totalAdded = 0;

            // recursively validated Id and Name
            var IdExists = FindNodeById(_root, NewAdditonTree.Id); 
            var NameExists = FindNodeByName(_root, NewAdditonTree.Name);
              

            // root node of new tree
            if (IdExists == null && NameExists == null)
            {
                //null == No duplicate found
                 _root.Children.Add(NewAdditonTree);

                // add the newly added node in assetAdded List for middleware logs
                 assetsAdded.Add(NewAdditonTree);
                totalAdded += TreeLength(NewAdditonTree);


            }
            else
            {
                // child nodes (remaning tree)
                foreach (var child in NewAdditonTree.Children)
                {

                    var nameExists = FindNodeByName(_root, child.Name);
                    var idExists = FindNodeById(_root, child.Id);
                    if (nameExists != null && idExists != null)
                    {
                        allGood = false;
                    }
                    else
                    {
                        _root.Children.Add(child);
                        assetsAdded.Add(child);
                        totalAdded+=TreeLength(child);


                    }

                }


            }


           

            if (totalAdded > 0)
            {
                _storage.SaveTree(_root); //save the tree
                return totalAdded;
            }
            return 0;
        }

        


    }
}
