using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asset_Management.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        // Navigation property (must be a property, not a field)
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public string Type { get; set; } = string.Empty;



        public string Message { get; set; } = string.Empty;

        public string? SenderName { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}
