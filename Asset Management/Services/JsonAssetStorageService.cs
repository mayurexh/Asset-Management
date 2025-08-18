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
    private readonly string _dataDirectory;

    
    public JsonAssetStorageService(IWebHostEnvironment env)
    {
        _dataDirectory = Path.Combine(env.ContentRootPath, "Data"); //Asset Management/assets.json
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

    public string GetVersionedFileName()
    {
        string latest_name = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        return Path.Combine(_dataDirectory, $"asset_json{latest_name}.json");
    }

    public Asset LoadTree()

    {
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };


        try
        {
            string latestfile = Path.Combine(_dataDirectory, "assets_latest.json");
            string json = File.ReadAllText(latestfile);
            return JsonConvert.DeserializeObject<Asset>(json, settings)
                   ?? new Asset { Id = "root", Name = "Root" };

        }
        catch (JsonException)
        {
            return null; // Invalid JSON
        }
    }


    public void SaveTree(Asset root)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        string filePath = GetVersionedFileName();

        string json = JsonConvert.SerializeObject(root, settings);

        File.WriteAllText(filePath, json); //write to latest version of a file

        File.WriteAllText(Path.Combine(_dataDirectory, "assets_latest.json"), json);
    }

}
