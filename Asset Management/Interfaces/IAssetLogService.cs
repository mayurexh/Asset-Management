using Asset_Management.Models;

namespace Asset_Management.Interfaces
{
    public interface IAssetLogService
    {

        void Log(string action, string? asset = null, string? signal = null);
    }
}
