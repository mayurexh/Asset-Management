using Asset_Management.Interfaces;
using Asset_Management.Services;
using System.Runtime.CompilerServices;

namespace Asset_Management.Extensions
{
    public static class AssetHierarchyServiceExtension 
    {
        public static IServiceCollection AddAssetHierarchyService(this IServiceCollection service, IConfiguration configuration) 
        {
            string serviceType = configuration["HierarchyServiceFlag"].ToLower();

            if (serviceType == "db")
            {
                service.AddScoped<IAssetHierarchyService, DbAssetHierarchyService>();

            }
            else
            {
                service.AddScoped<IAssetHierarchyService, AssetHierarchyService>();
            }
            return service;

        }
    }
}
