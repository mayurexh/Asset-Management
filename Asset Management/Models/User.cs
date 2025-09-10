using System.ComponentModel.DataAnnotations;

namespace Asset_Management.Models
{
    
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string Username { get; set; }

        [Required]
        [MaxLength(32)]

        public string Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Viewer";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    }
}
