using Asset_Management.Interfaces;
using Asset_Management.Models;
using Asset_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.ObjectPool;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Serialization;

namespace Asset_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetHierarchyController : ControllerBase
    {
        private readonly IAssetHierarchyService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IAssetStorageService _storage;
        private readonly IUploadLogService _uploadlog;
        public AssetHierarchyController(IAssetHierarchyService service, IWebHostEnvironment env, IConfiguration configuration, IAssetStorageService storage, IUploadLogService uploadlog)
        {
            _service = service;
            _storage = storage;
            _env = env;
            _configuration = configuration;
            _uploadlog = uploadlog;
        }

        [HttpGet]
        [Produces("application/json", "application/xml")]
        public IActionResult GetHierarchy()
        {
            var tree = _service.GetHierarchy();
            return Ok(tree);
        }


        [HttpPost]
        public IActionResult AddNode([FromBody] AssetAddRequest request)
        {
            // validate Id, Name and Children
            // Regex patterns
            string idPattern = @"^[a-zA-Z0-9_-]{1,20}$";
            string namePattern = @"^[a-zA-Z0-9 ]{1,30}$";

            // Validate Id
            if (string.IsNullOrWhiteSpace(request.Id) || !Regex.IsMatch(request.Id, idPattern))
                return BadRequest("Invalid ID. Must be alphanumeric and can contain _ or -, max 20 characters.");

            // Validate ParentId
            if (string.IsNullOrWhiteSpace(request.ParentId) || !Regex.IsMatch(request.ParentId, idPattern))
                return BadRequest("Invalid Parent ID. Must be alphanumeric and can contain _ or -, max 20 characters.");

            // Validate Name
            if (string.IsNullOrWhiteSpace(request.Name) || !Regex.IsMatch(request.Name, namePattern))
                return BadRequest("Invalid Name. Only letters, numbers, and spaces are allowed, max 30 characters.");

            var newAsset = new Asset
            {
                Id = request.Id,
                Name = request.Name,
                Children = new List<Asset>()
            };

            bool success = _service.AddNode(request.ParentId, newAsset);
            if (!success)
                return BadRequest("Parent not found or ID already exists or Name already exists.");

            return Ok("Node added successfully.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteNode(string id)
        {
            bool success = _service.RemoveNode(id);
            if (!success)
                return BadRequest("Node not found or cannot delete root node.");

            return Ok("Node deleted successfully.");
        }

        [HttpGet("GetCount")]
        public IActionResult GetCount()
        {
            int count = _service.TreeLength(_storage.LoadTree());
            return Ok($"{count}");
        }

        [HttpPost("UploadExistingTree")]
        public IActionResult UploadInExisitng(IFormFile file)
        {
            try
            {
                if (file.Length == 0 || file == null)
                {
                    return BadRequest("File Invalid");

                }
                using var sr = new StreamReader(file.OpenReadStream());

                var content = sr.ReadToEnd();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var NewAdditonTree = JsonSerializer.Deserialize<Asset>(content, options);

                if (NewAdditonTree == null)
                {
                    return BadRequest("Invalid JSON format");
                }

                //check if root Node is null
                if (NewAdditonTree.Id == null)
                {
                    return BadRequest("Root node cannot be null");
                }

                int result = _service.MergeTree(NewAdditonTree);
                _uploadlog.UpdateLog(file.FileName, "merged");
                HttpContext.Items["assetsAdded"] = AssetHierarchyService.assetsAdded;
                return Ok(result);

                
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Error processing file. {ex.Message}");

            }
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> UploadHierarchy(IFormFile file)
        {

            if (file.Length == 0 || file == null)
            {
                return BadRequest("File Invalid");

            }
            try
            {
                
                var FileExtension = System.IO.Path.GetExtension(file.FileName);



                //if file is of json format
                if (FileExtension == ".json")
                {
                    //set config StorageFlag to json if user is going to upload file in json
                    _configuration["StorageFlag"] = "json";

                    // file is acquired from a HTTP req thats why we use IFormFile and allow to read the file by OpenReadStream()
                    using var sr = new StreamReader(file.OpenReadStream());

                    var content = await sr.ReadToEndAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var NewTree = JsonSerializer.Deserialize<Asset>(content, options);
                    if (NewTree == null)
                    {
                        return BadRequest("Invalid JSON format");
                    }

                    //check if root Node is null
                    if (NewTree.Id == null)
                    {
                        return BadRequest("Root node cannot be null");
                    }
                    _service.ReplaceTree(NewTree);
                    _uploadlog.UpdateLog(file.FileName, "uploaded");


                }
                //if file is of xml format
                else if(FileExtension == ".xml")
                {
                    //set config StorageFlag to xml if user is going to upload file in xml

                    _configuration["StorageFlag"] = "xml";

                    using var stream = new StreamReader(file.OpenReadStream());
                    XmlSerializer serializer = new XmlSerializer(typeof(Asset));


                    var NewTree = (Asset)serializer.Deserialize(stream);
                    if (NewTree == null)
                    {
                        return BadRequest("Invalid XML format");
                    }

                    //check if root Node is null
                    if (NewTree.Id == null)
                    {
                        return BadRequest("Root node cannot be null");
                    }
                    _service.ReplaceTree(NewTree);
                    _uploadlog.UpdateLog(file.FileName, "uploaded");

                }






                return Ok($"{file.Name} has been successfully uploaded");

                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing file. {ex.Message}");
            }

        }

        [HttpGet("DownloadFile/{format}")]
        public IActionResult DownloadFile(string format)
        {

            string[] formats = { "json", "xml" }; // string format should only be json or xml
            Console.WriteLine(format);
            if(!formats.Contains(format))
            {
                return BadRequest("Only files with json and xml format could be downloaded");
            }
            string FilePath = Path.Combine(_env.ContentRootPath, $"assets.{format}"); //dynamically assign extension of file using format 


            if(!System.IO.File.Exists(FilePath))
            {
                return NotFound("File does not exists");
            }

            //save all the bytes of "Root/assets.json" in FileByets array
            byte[] FileBytes = System.IO.File.ReadAllBytes(FilePath);

            //specify content type of the file 
            string ContentType;
            if (format == "json")
            {
                ContentType = "application/json";

            }
            else
            {
                ContentType = "application/xml";
            }


                return File(FileBytes, ContentType, "Assets");

        }

        [HttpGet("ImportFileLogs")]
        public IActionResult ImportFileLogs()
        {
            var logs = _uploadlog.GetUploadLogs();
            if (logs != null)
            {
                return Ok(logs);
            }
            return Ok("No import logs found");
        }
    }

    // DTO for POST request
    public class AssetAddRequest
    {
        [Required(ErrorMessage = "ID is required.")]
        [RegularExpression(@"^[a-zA-Z0-9_-]{1,30}$",
            ErrorMessage = "Invalid ID. Must be alphanumeric and can contain _ or -, max 30 characters.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$",
            ErrorMessage = "Invalid Name. Only letters, numbers, and spaces are allowed, max 30 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parent ID is required.")]
        [RegularExpression(@"^[a-zA-Z0-9_-]{1,30}$",
            ErrorMessage = "Invalid Parent ID. Must be alphanumeric and can contain _ or -, max 30 characters.")]
        public string ParentId { get; set; } = string.Empty;
    }
}
