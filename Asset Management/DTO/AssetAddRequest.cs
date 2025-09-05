using System.ComponentModel.DataAnnotations;

namespace Asset_Management.DTO
{
    public class AssetAddRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$",
            ErrorMessage = "Invalid Name. Only letters, numbers, and spaces are allowed, max 30 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parent ID is required.")]

        public int ParentId { get; set; }
    }
}
