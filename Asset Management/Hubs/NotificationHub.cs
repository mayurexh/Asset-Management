using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Asset_Management.Hubs
{
    public class NotificationHub : Hub
    {

        private static readonly Dictionary<string, List<string>> _connections
        = new Dictionary<string, List<string>>();

        public override async Task OnConnectedAsync()
        {

            //save user to _connections dictionary in order to do a send notification to every except that user
            var user = Context.UserIdentifier;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            // Debug: Print all available user information
            Console.WriteLine($"UserIdentifier: {Context.UserIdentifier}");
            Console.WriteLine($"User Name: {Context.User?.Identity?.Name}");
            Console.WriteLine($"User Role: {userRole}");
            Console.WriteLine($"Is Authenticated: {Context.User?.Identity?.IsAuthenticated}");

            if (string.IsNullOrEmpty(user))
            {
                // Log this for debugging
                Console.WriteLine("User identifier is null or empty");
                await base.OnConnectedAsync();
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                if (userRole == "Admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{userRole}");
                    Console.WriteLine("Role group added");
                }
                else
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{userRole}");
                    Console.WriteLine("Role group added");


                }


            }



            if (!_connections.ContainsKey(user))
                _connections[user] = new List<string>();

            _connections[user].Add(Context.ConnectionId);



            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.UserIdentifier;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            

            if (string.IsNullOrEmpty(user))
            {
                // Log this for debugging
                Console.WriteLine("User identifier is null or empty");
                await base.OnConnectedAsync();
                return;
            }
            _connections[user]?.Remove(Context.ConnectionId);

            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Role_{userRole}");
                Console.WriteLine("User remov");
            }
                
            await base.OnDisconnectedAsync(exception);
        }

        public static List<string> GetConnections(string user)
        {
            if (_connections.ContainsKey(user))
                return _connections[user];
            return new List<string>();
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
