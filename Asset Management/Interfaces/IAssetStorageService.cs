using Asset_Management.Models;

namespace Asset_Management.Interfaces
{
    public interface IAssetStorageService
    {
        Asset LoadTree();
        void SaveTree(Asset root);

        Asset ParseTree(string content); //content here is the new json file that user is uploading converted to a string.
    }
}
