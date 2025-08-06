using Microsoft.AspNetCore.Mvc;
using Asset_Management.Models;
using Asset_Management.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Asset_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetHierarchyController : ControllerBase
    {
        private readonly AssetHierarchyService _service;
        private readonly IWebHostEnvironment _env;

        public AssetHierarchyController(AssetHierarchyService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
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
            var newAsset = new Asset
            {
                Id = request.Id,
                Name = request.Name,
                Children = new List<Asset>()
            };

            bool success = _service.AddNode(request.ParentId, newAsset);
            if (!success)
                return BadRequest("Parent not found or ID already exists.");

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


        [HttpPost("Upload")]
        public async Task<IActionResult> UploadHierarchy(IFormFile file)
        {

            if (file.Length == 0 || file == null)
            {
                return BadRequest("File Invalid");

            }
            try
            {
                // file is acquired from a HTTP req thats why we use IFormFile and allow to read the file by OpenReadStream()
                using var sr = new StreamReader(file.OpenReadStream());
                

                var content = await sr.ReadToEndAsync();

                //var FileExtension = System.IO.Path.GetExtension(file.FileName);


                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var NewTree = JsonSerializer.Deserialize<Asset>(content, options);
                if(NewTree == null)
                {
                    return BadRequest("Invalid JSON format");
                }

                //check if root Node is null
                if(NewTree.Id == null)
                {
                    return BadRequest("Root node cannot be null");
                }

                _service.ReplaceTree(NewTree);
                return Ok($"{file.Name} has been successfully uploaded");

                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing file. {ex.Message}");
            }

        }

        [HttpGet("DownloadFile")]
        public IActionResult DownloadFile()
        {
            string FilePath = Path.Combine(_env.ContentRootPath, "assets.json");

            if(!System.IO.File.Exists(FilePath))
            {
                return NotFound("File does not exists");
            }

            //save all the bytes of "Root/assets.json" in FileByets array
            byte[] FileBytes = System.IO.File.ReadAllBytes(FilePath);

            //specify content type of the file 
            string ContentType = "application/json";


            return File(FileBytes, ContentType, "Assets");

        }
    }

    // DTO for POST request
    public class AssetAddRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
    }
}
