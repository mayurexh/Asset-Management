using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Asset_Management.Services;
using Asset_Management.Interfaces;

namespace Asset_Management.Extensions
{
    public static class StorageServiceExtension
    {

        public static IServiceCollection AddStorageServices(this IServiceCollection service, IConfiguration configuration)
        {
            string FileType = configuration["StorageFlag"].ToLower();
            if (FileType == "xml")
            {
                service.AddSingleton<IAssetStorageService, XmlAssetStorageService>();
            }
            else
            {
                service.AddSingleton<IAssetStorageService, JsonAssetStorageService>();
            }
            return service;

        }
    }
}
