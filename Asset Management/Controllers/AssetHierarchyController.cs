using Asset_Management.Interfaces;
using Asset_Management.Models;
using Asset_Management.Services;
using Asset_Management.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        public IActionResult GetHierarchy()
        {
            var tree = _service.GetHierarchy();
            if (tree == null)
            {
                return BadRequest("No Asset Hierarchy Present. Please upload to start.");
            }
            return Ok(tree);
        }

        //Get toatal Assets in the Hierarchy Tree
        [HttpGet("TotalAssets")]
        public IActionResult GetTotalAssets()
        {
            var tree = _service.GetHierarchy();
            if (tree == null)
            {
                return BadRequest("No tree present");
            }
            int totalAssets = _service.TotalAsset(tree);
            totalAssets -= 1; //Exclude the root node
            return Ok($"{totalAssets}");
        }

        [HttpPost]
        public IActionResult AddNode([FromBody] AssetAddRequest request)
        {
            if (!ModelState.IsValid)
            {
                // Extract all validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                Console.Write(errors);
                return BadRequest(new { Errors = errors });
            }

            var newAsset = new Asset
            {
                Name = request.Name,
                Children = new List<Asset>()
            };
            Console.WriteLine($"{request.Name}, {request.ParentId}");

            bool success = _service.AddNode(request.ParentId, newAsset);
            if (!success)
            {
                Console.WriteLine("Not successfull from AddNode Action");
                // Return same structured error as ModelState
                var fieldErrors = new Dictionary<string, string[]>
        {
            { "parentId", new[] { "Parent not found or ID already exists or Name already exists." } }
        };
                return BadRequest(new { errors = fieldErrors });
            }

            return Ok("Node added successfully.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteNode(int id)
        {
            bool success = _service.RemoveNode(id);
            if (!success)
                return BadRequest("Node cannot be deleted.");

            return Ok("Node deleted successfully.");
        }

        [HttpPut("Update/{id}")]
        public IActionResult UpdateNode(int id, string name)
        {
            bool success = _service.UpdateNode(id, name);

            if (!success)
                return BadRequest("Name already present in hierarchy ");
            return Ok("Name updated");

        }


        [HttpPost("UploadExistingTree")]
        public IActionResult UploadInExisting(IFormFile file)
        {
            var FileExtension = System.IO.Path.GetExtension(file.FileName);
            var storageExtension = "." + _configuration["StorageFlag"];
            if (FileExtension != storageExtension)
            {
                return BadRequest($"Invalid File format, please upload a {_configuration["StorageFlag"]} file");
            }
            try
            {
                if (file.Length == 0 || file == null)
                {
                    return BadRequest("File Invalid");
                }
                using var sr = new StreamReader(file.OpenReadStream());
                var content = sr.ReadToEnd();
                Asset NewAdditionTree = _storage.ParseTree(content);

                // ✅ Remove the null check since int IDs default to 0
                // Reset IDs to 0 so EF Core can generate new ones
                //ResetTreeIds(NewAdditionTree);

                int result = _service.MergeTree(NewAdditionTree);
                _uploadlog.UpdateLog(file.FileName, "merged");
                HttpContext.Items["assetsAdded"] = AssetHierarchyService.assetsAdded;
                return Ok(result);

            }
            catch (InvalidFileFormatException ex)
            {
                return BadRequest($"Invalid File format, please upload a valid {_configuration["StorageFlag"]} file");
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }

        private void ResetTreeIds(Asset node)
        {
            node.Id = 0; // Let EF Core generate new ID
            foreach (var child in node.Children)
            {
                ResetTreeIds(child);
            }
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> UploadHierarchy(IFormFile file)
        {

            if (file.Length == 0 || file == null)
            {
                return BadRequest("File Invalid");

            }

            var FileExtension = System.IO.Path.GetExtension(file.FileName);

            // check if file uploaded by user is of type _configuration["StorageFlag"] as based on the StorageFlag storage service is injected
            // at start of the program
            var storageExtension = "."+_configuration["StorageFlag"]; // "." is added because GetExtension method return extension with a . (eg. .json/.xml)
            Type type = storageExtension.GetType();
            Console.WriteLine(type);
            if (FileExtension != storageExtension)
            {
                return BadRequest($"Invalid File format, please upload a {_configuration["StorageFlag"]} file");
            }

            else
            {
                using var sr = new StreamReader(file.OpenReadStream());

                var content = await sr.ReadToEndAsync();
                try
                {
                    //check the validation and format of the tree
                    var newRoot = _storage.ParseTree(content);
                    ValidateAssetRecursively(newRoot);
                    Console.WriteLine("Parsing Done");
                    //PopulateParentIds.AssignParentIds(newRoot);
                    foreach(var child in newRoot.Children)
                    {
                        Console.WriteLine($"Parent: {child.ParentId}, Name: {child.Name}, Id: {child.Id}");
                    }
                    _service.ReplaceTree(newRoot);
                    _uploadlog.UpdateLog(file.FileName, "uploaded"); //updateLogService
                    return Ok("File uploaded successfully");
                }
                catch (InvalidFileFormatException ex)
                {
                    return BadRequest($"{ex.Message}");
                }catch(DbUpdateException ex)
                {
                    return BadRequest($"Database Saving exception");
                }
                catch(ValidationException ex)
                {
                    return BadRequest($"Invalid json format, please check for missing fields");
                }
                catch(Exception ex)
                {
                    return BadRequest($"{ex.Message}"); 
                }


            }
        }
        private void ValidateAssetRecursively(Asset asset)
        {
            // Validate current asset
            var context = new ValidationContext(asset);
            Validator.ValidateObject(asset, context, validateAllProperties: true);

            // Validate signals
            foreach (var signal in asset.Signals)
            {
                var signalContext = new ValidationContext(signal);
                Validator.ValidateObject(signal, signalContext, validateAllProperties: true);
            }

            // Validate children
            foreach (var child in asset.Children)
            {
                ValidateAssetRecursively(child);
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
            string FilePath = Path.Combine(_env.ContentRootPath,"Data", $"assets_latest.{format}"); //dynamically assign extension of file using format 


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
       

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$",
            ErrorMessage = "Invalid Name. Only letters, numbers, and spaces are allowed, max 30 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parent ID is required.")]

        public int ParentId { get; set; }
    }
}
