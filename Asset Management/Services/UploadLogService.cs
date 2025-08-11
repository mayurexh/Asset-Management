using Asset_Management.Interfaces;
namespace Asset_Management.Services
{
    public class UploadLogEntry
    {
        public string FileName { get; set; }
        public string ImportType { get; set; }
    }
    public class UploadLogService : IUploadLogService
    {
        //private readonly Dictionary<string, DateTime> _uploadFileLog = new Dictionary<string, DateTime>();

        //making datetime as the key which won't throw issues for file with same names

        private readonly Dictionary<DateTime, UploadLogEntry> _uploadFileLog = new Dictionary<DateTime, UploadLogEntry>();
        public UploadLogService() 
        { 
        }

        public void UpdateLog(string filename, string importType)
        {
            _uploadFileLog.Add(DateTime.Now, new UploadLogEntry
            {
                FileName = filename,
                ImportType = importType
            });
        }

        public Dictionary<DateTime, UploadLogEntry> GetUploadLogs()
        {
            return _uploadFileLog;
        }


    }
}
