namespace Asset_Management.Interfaces
{
    using Asset_Management.Models;

    public interface IAssetHierarchyService
    {
        Asset GetHierarchy();
        bool AddNode(int parentId, Asset newNode);
        bool AddToRoot(string assetName);

        bool RemoveNode(int nodeId);

        bool UpdateNode(int nodeId, string newName);

        bool CheckDuplicated(Asset Node);
        int TotalAsset(Asset Node);
        void ReplaceTree(Asset newRoot);
        int MergeTree(Asset AddtionalNode);
        int TreeLength(Asset Node);
    }
}
