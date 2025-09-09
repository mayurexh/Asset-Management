namespace Asset_Management.Interfaces
{
    using Asset_Management.Models;

    public interface IAssetHierarchyService
    {
        Asset GetHierarchy();
        Task<bool> AddNode(int parentId, Asset newNode);
        Task<bool> AddToRoot(string assetName);

        Task<bool> RemoveNode(int nodeId);

        Task<bool> UpdateNode(int nodeId, string newName);

        Task ReorderNode(int assetId, int parentId);
        bool CheckDuplicated(Asset Node);
        int TotalAsset(Asset Node);
        void ReplaceTree(Asset newRoot);
        int MergeTree(Asset AddtionalNode);
        int TreeLength(Asset Node);
    }
}
