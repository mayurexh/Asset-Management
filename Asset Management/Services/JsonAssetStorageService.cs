using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Text.Json;

public class JsonAssetStorageService : IAssetStorageService
{
    private readonly string _dataFile;

    public JsonAssetStorageService(IWebHostEnvironment env)
    {
        _dataFile = Path.Combine(env.ContentRootPath, "assets.json"); //Asset Management/assets.json
    }

    public Asset LoadTree()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (File.Exists(_dataFile))
        {
            string json = File.ReadAllText(_dataFile);
            return JsonSerializer.Deserialize<Asset>(json, options)
                   ?? new Asset { Id = "root", Name = "Root" };
        }
        else
        {
            var root = new Asset { Id = "root", Name = "Root" };
            SaveTree(root);
            return root;
        }
    }

    public void SaveTree(Asset root)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(root, options);
        File.WriteAllText(_dataFile, json);
    }
}
