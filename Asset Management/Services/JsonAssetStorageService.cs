using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


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
    private readonly AssetDbContext _dbContext;
    private readonly IConfiguration _configuration;


    public JsonAssetStorageService(IWebHostEnvironment env, AssetDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _dataDirectory = Path.Combine(env.ContentRootPath, "Data"); //Asset Management/assets.json
    }

    public Asset ParseTree(string content)
    {
        try
        {
            var newRoot = JsonConvert.DeserializeObject<Asset>(content, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore //only throws error/ignore for extra fields and not missing members
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
            throw new InvalidFileFormatException("Invalid File, extra fields present only Name, Children and Signals allowed" , ex);
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
                   ?? new Asset { Name = "Root" };

        }
        catch (JsonException)
        {
            return null; // Invalid JSON
        }
    }


    public void SaveTree(Asset root, string? action = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            action = "None";
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        string filePath = GetVersionedFileName();

        string json = JsonConvert.SerializeObject(root, settings);


        //save versoning to database
        if (_configuration["HierarchyServiceFlag"].ToLower() == "db")
        {
            HierarchyVersion hieararchy = new HierarchyVersion
            {
                Action = action,
                EditedTime = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                SnapshotJson = json

            };

            //delete file version after n number of entries 
            var count = _dbContext.HierarchyVersions.Count();
            int limit = 5;
            if (count >= limit)
            {
                var rowsToDelete = _dbContext.HierarchyVersions.OrderByDescending(h => h).Take(limit).ToList();
                _dbContext.HierarchyVersions.RemoveRange(rowsToDelete);
            }
            
            

            _dbContext.HierarchyVersions.Add(hieararchy);
            _dbContext.SaveChanges();

        }

        

        File.WriteAllText(filePath, json); //write to latest version of a file

        File.WriteAllText(Path.Combine(_dataDirectory, "assets_latest.json"), json);
    }

}
