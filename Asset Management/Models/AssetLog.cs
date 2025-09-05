using System.ComponentModel.DataAnnotations;

namespace Asset_Management.Models
{
    public class AssetLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; }

        public string? Asset {get; set;}

        public string? Signal {get; set;}

        public string Action { get; set; }

        public DateTime LogTime { get; set; } = DateTime.UtcNow;

    }
}
