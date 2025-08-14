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

    public Asset? LoadTree()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (!File.Exists(_dataFile))
        {
            return null; // File doesn't exist
        }

        string json = File.ReadAllText(_dataFile);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null; // File empty
        }

        try
        {
            return JsonSerializer.Deserialize<Asset>(json, options);
        }
        catch (JsonException)
        {
            return null; // Invalid JSON
        }
    }


    public void SaveTree(Asset root)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(root, options);
        File.WriteAllText(_dataFile, json);
    }

    public void DeleteTreeFile()
    {
        if (File.Exists(_dataFile))
        {
            System.IO.File.WriteAllText(_dataFile, string.Empty);
        }
    }
}
