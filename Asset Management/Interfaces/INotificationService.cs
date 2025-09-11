using Asset_Management.DTO;

namespace Asset_Management.Interfaces
{
    public interface INotificationService
    {
        Task BroadcastToAdminsAndViewers(string currentUserId,  AssetNotificationDTO notification);
    }
}
