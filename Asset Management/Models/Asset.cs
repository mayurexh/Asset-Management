using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Asset_Management.Models
{
    [XmlRoot("asset")]  
    public class Asset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [XmlAttribute("id")] //specifying xml attribute so that xml serializer doesn't throw unknow attribute error as it excepts attributes
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [JsonIgnore]
        [XmlIgnore] 
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        [JsonIgnore]
        [XmlIgnore]
        public Asset? Parent { get; set; }

        [XmlElement("asset")] //include case insensitiviy
        public List<Asset> Children { get; set; } = new List<Asset>();
    }
}   
