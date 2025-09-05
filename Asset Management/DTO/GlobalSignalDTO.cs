using System.ComponentModel.DataAnnotations;

namespace Asset_Management.DTO
{
    public class GlobalSignalDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$",
            ErrorMessage = "Invalid Name. Only letters, numbers, and spaces are allowed, max 30 characters.")]
        public string Name { get; set; }
        public string ValueType { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$",
            ErrorMessage = "Invalid Description. Only letters, numbers, and spaces are allowed, max 30 characters.")]
        public string? Description { get; set; }

    }
}
