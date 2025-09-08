using Microsoft.AspNetCore.SignalR;

namespace Asset_Management.Hubs
{
    public class NotificationHub : Hub
    {
        
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
