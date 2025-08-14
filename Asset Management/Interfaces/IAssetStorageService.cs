using Asset_Management.Models;

namespace Asset_Management.Interfaces
{
    public interface IAssetStorageService
    {
        Asset LoadTree();
        void SaveTree(Asset root);
        void DeleteTreeFile();
    }
}
