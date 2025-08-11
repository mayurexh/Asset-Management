using Asset_Management.Services;

namespace Asset_Management.Interfaces
{
    public interface IUploadLogService
    {
        void UpdateLog(string filename, string importType);
        Dictionary<DateTime, UploadLogEntry> GetUploadLogs();

    }
}
