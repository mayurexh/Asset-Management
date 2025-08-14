using Asset_Management.Interfaces;
using Asset_Management.Models;
using Newtonsoft.Json;


public class InvalidFileFormatException : Exception
{
    //storing innerException to track what was the original exception that was triggered
    public InvalidFileFormatException(string message, Exception innerException = null) : base(message, innerException)
    {

    }
}
public class JsonAssetStorageService : IAssetStorageService
{
    private readonly string _dataFile;

    
    public JsonAssetStorageService(IWebHostEnvironment env)
    {
        _dataFile = Path.Combine(env.ContentRootPath, "assets.json"); //Asset Management/assets.json
    }

    public Asset ParseTree(string content)
    {
        try
        {
            var newRoot = JsonConvert.DeserializeObject<Asset>(content, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            });

            if (newRoot == null)
            {
                throw new InvalidOperationException("Root object is null.");
            }

            return newRoot;
        }
        catch (InvalidOperationException ex)
        {
            throw; // Let controller handle and return 400
        }
        catch (JsonSerializationException ex)
        {
            throw new InvalidFileFormatException("Invalid File", ex);
        }
    }

    public Asset LoadTree()
    {
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        if (File.Exists(_dataFile))
        {
            string json = File.ReadAllText(_dataFile);
            return JsonConvert.DeserializeObject<Asset>(json, settings)
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
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        string json = JsonConvert.SerializeObject(root, settings);
        File.WriteAllText(_dataFile, json);
    }
}
