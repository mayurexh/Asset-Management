using Microsoft.AspNetCore.Mvc;
using Asset_Management.Models;
using Asset_Management.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace Asset_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetHierarchyController : ControllerBase
    {
        private readonly AssetHierarchyService _service;

        public AssetHierarchyController(AssetHierarchyService service)
        {
            _service = service;
        }

        [HttpGet]
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


        [HttpPost("upload")]
        public async Task<IActionResult> UploadHierarchy(IFormFile file)
        {

            if (file.Length == 0 || file == null)
            {
                return BadRequest("File Invalid");

            }
            try
            {
                // file is acquired from a HTTP req thats why we use IFormFIle and allow to read the file by OpenReadStream()
                using var sr = new StreamReader(file.OpenReadStream());

                var content = await sr.ReadToEndAsync();

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
    }

    // DTO for POST request
    public class AssetAddRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
    }
}
