namespace Asset_Management.Interfaces
{
    using Asset_Management.Models;

    public interface IAssetHierarchyService
    {
        Asset GetHierarchy();
        bool AddNode(string parentId, Asset newNode);
        bool RemoveNode(string nodeId);

        bool CheckDuplicated(Asset Node);
        int TotalAsset(Asset Node);
        void ReplaceTree(Asset newRoot);
        int MergeTree(Asset AddtionalNode);
        int TreeLength(Asset Node);
    }
}
