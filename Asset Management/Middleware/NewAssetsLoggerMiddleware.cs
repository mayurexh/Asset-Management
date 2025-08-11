using System.Text.Json;

namespace Asset_Management.Middleware
{
    public class NewAssetsLoggerMiddleware
    {
        private readonly RequestDelegate _next;

        public NewAssetsLoggerMiddleware(RequestDelegate next)
        {
            _next = next;
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

                    Console.WriteLine("=== Added Assets ===");
                    Console.WriteLine(json);
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
