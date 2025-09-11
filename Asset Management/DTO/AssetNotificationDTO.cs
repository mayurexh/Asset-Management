namespace Asset_Management.DTO
{
    public class AssetNotificationDTO
    {
        public string Type { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        
        public string? ParentName { get; set; }
        public string? Name { get; set; }
        public string? OldName { get; set; }
        public string? NewName { get; set; }
    }
}
