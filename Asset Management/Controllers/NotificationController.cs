using Asset_Management.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Asset_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensure user is authenticated
    public class NotificationController : Controller
    {
        private readonly AssetDbContext _dbContext;
        public NotificationController(AssetDbContext dbContext)
        {

            _dbContext = dbContext;
        }

        private string? GetCurrentUser()
        {
            return HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        private string? GetCurrentUserID()
        {
            return HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


        }

        // GET: api/notification/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                // Get current user ID (assuming you have a method to get this)
                var currentUserId = GetCurrentUserID(); // Replace with your method

                // Ensure user can only access their own notifications
                if (currentUserId != userId.ToString())
                {
                    return Forbid();
                }

                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new
                    {
                        n.Id,
                        n.Type,
                        n.Message,
                        n.SenderName,
                        n.IsRead,
                        n.CreatedAt
                    })
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching notifications", error = ex.Message });
            }
        }


        //PUT: api/notification/mark-read
       [HttpPut("mark-read")]
        public async Task<IActionResult> MarkNotificationsAsRead([FromBody] List<int> notificationIds)
        {
            try
            {
                Console.WriteLine($"Backend: Received request to mark notifications as read: {string.Join(", ", notificationIds)}");

                var currentUserId = int.Parse(GetCurrentUserID()); // Replace with your method
                Console.WriteLine($"Backend: Current user ID: {currentUserId}");

                var notifications = await _dbContext.Notifications
                    .Where(n => notificationIds.Contains(n.Id) && n.UserId == currentUserId)
                    .ToListAsync();

                Console.WriteLine($"Backend: Found {notifications.Count} notifications to mark as read");

                if (!notifications.Any())
                {
                    Console.WriteLine("Backend: No notifications found to update");
                    return NotFound(new { message = "No notifications found to update" });
                }

                foreach (var notification in notifications)
                {
                    Console.WriteLine($"Backend: Marking notification {notification.Id} as read (was {notification.IsRead})");
                    notification.IsRead = true;
                }

                var changesCount = await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Backend: Saved {changesCount} changes to database");

                return Ok(new { message = $"Marked {notifications.Count} notifications as read", count = notifications.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backend Error: {ex.Message}");
                Console.WriteLine($"Backend Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error updating notifications", error = ex.Message });
            }
        }

        // PUT: api/notification/mark-all-read/{userId}
        [HttpPut("mark-all-read/{userId}")]
        public async Task<IActionResult> MarkAllNotificationsAsRead(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserID(); // Replace with your method

                // Ensure user can only update their own notifications
                if (currentUserId != userId.ToString())
                {
                    return Forbid();
                }

                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating notifications", error = ex.Message });
            }
        }

        // DELETE: api/notification/clear/{userId}
        [HttpDelete("clear/{userId}")]
        public async Task<IActionResult> ClearAllNotifications(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserID(); 

                // Ensure user can only clear their own notifications
                if (currentUserId != userId.ToString())
                {
                    return Forbid();
                }

                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                _dbContext.Notifications.RemoveRange(notifications);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "All notifications cleared" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error clearing notifications", error = ex.Message });
            }
        }


    }
}
