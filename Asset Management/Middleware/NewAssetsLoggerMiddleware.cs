using Asset_Management.Services;
using System.Text.Json;
using Serilog;

namespace Asset_Management.Middleware
{
    public class NewAssetsLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<NewAssetsLoggerMiddleware> _logger;
        public NewAssetsLoggerMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<NewAssetsLoggerMiddleware> logger )
        {
            _next = next;
            _env = env;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/AssetHierarchy/UploadExistingTree"))
            {
                await _next(context);



                // After controller executes, check if assetsAdded was set
                if (context.Items.TryGetValue("assetsAdded", out var list) && list is IEnumerable<object> addedAssets)
                {
                    string json = JsonSerializer.Serialize(addedAssets, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    //Console.WriteLine("=== Added Assets ===");
                    //Console.WriteLine(json);
                    //_logger.LogInformation($"Added assets: {json}");
                    //clear the assets Added list so that middleware includes only newly added assets
                    AssetHierarchyService.assetsAdded.Clear();
                }
            }
            else
            {
                // For other requests, just continue pipeline
                await _next(context);
            }

        }
    }
}
