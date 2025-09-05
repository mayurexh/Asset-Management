using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Asset_Management.Models
{
    public class Signal
    {

        [Key]
        public int Id { get; set; }
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9 ]{1,30}$")]
        public string Name { get; set; }
        [Required]
        public string ValueType { get; set; }
        public string? Description { get; set; }
        public int AssetId { get; set; }

        [JsonIgnore]
        public Asset Asset { get; set; }

    }
}
