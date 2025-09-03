using System.ComponentModel.DataAnnotations;

namespace Asset_Management.DTO
{
    public class LoginDTO
    {
        [Required]
        [MinLength(2)]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$",
            ErrorMessage = "Username can only contain letters, numbers, hyphens (-), and underscores (_) (upto 30 characters)")]
        public string Username { get; set; }
        
        [Required]
        [MinLength(2)]
        [MaxLength(32)]
        public string Password { get; set; }
    }
}
