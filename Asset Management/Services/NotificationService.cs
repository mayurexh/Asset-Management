using Asset_Management.DTO;
using Asset_Management.Hubs;
using Asset_Management.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Asset_Management.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        public NotificationService(IHubContext<NotificationHub> hubContext) 
        { 
        }
        public async Task BroadcastToAdminsAndViewers(string currentUserId,  AssetNotificationDTO notification)
        {
            var connectionIds = NotificationHub.GetConnections(currentUserId) ?? new List<string>();

            Console.WriteLine("FROM BROADCAST");
            Console.WriteLine(currentUserId);
            Console.WriteLine($"{notification.Name}");
            foreach(string id in connectionIds)
            {
                Console.WriteLine(id);
            }
            // admins (exclude actor)
            await _hubContext.Clients
                .GroupExcept("Role_Admin", connectionIds)
                .SendAsync("RecieveAssetNotification", notification);

            // viewers (mask user as "Admin")
            var viewerNotification = new AssetNotificationDTO
            {
                Type = notification.Type,
                User = "Admin", // mask
                Name = notification.Name,
                OldName = notification.OldName,
                NewName = notification.NewName
            };

            await _hubContext.Clients
                .Group("Role_Viewer")
                .SendAsync("RecieveAssetNotification", viewerNotification);
        }
    }
}
