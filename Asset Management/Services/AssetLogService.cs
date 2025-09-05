using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using System.Security.Claims;

namespace Asset_Management.Services
{
    public class AssetLogService : IAssetLogService
    {
        private readonly AssetDbContext _dbContext;

        //By default httpContext is available only to controllers and middleware, IHttpContextAccessor is 
        //a special service that provides it outside of controllers and middleware.
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AssetLogService(AssetDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            
        }

        public void Log(string action, string? asset = null, string? signal = null)
        {
            if (string.IsNullOrWhiteSpace(asset))
                asset = string.Empty;

            if (string.IsNullOrWhiteSpace(signal))
                signal = string.Empty;


            int.TryParse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int user);

            AssetLog assetLog = new AssetLog
            {
                UserId = user,
                Action = action,
                Asset = asset,
                Signal = signal,
                LogTime = DateTime.UtcNow


            };
            _dbContext.AssetLogs.Add(assetLog);
            _dbContext.SaveChanges();

        }






    }

}
