using Microsoft.AspNetCore.Mvc;
using Asset_Management.Models;
using Asset_Management.Services;

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

        // ✅ GET /api/AssetHierarchy
        [HttpGet]
        public IActionResult GetHierarchy()
        {
            var tree = _service.GetHierarchy();
            return Ok(tree);
        }

        // ✅ POST /api/AssetHierarchy
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

        // ✅ DELETE /api/AssetHierarchy/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteNode(string id)
        {
            bool success = _service.RemoveNode(id);
            if (!success)
                return BadRequest("Node not found or cannot delete root node.");

            return Ok("Node deleted successfully.");
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
