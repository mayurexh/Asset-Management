using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace Asset_Management.Models
{
    [XmlRoot("asset")]
    public class Asset
    {
        [XmlAttribute("id")] //specifying xml attribute so that xml serializer doesn't throw unknow attribute error as it excepts attributes
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        public string? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public Asset? Parent { get; set; }

        [XmlElement("asset")] //include case insensitiviy
        public List<Asset> Children { get; set; } = new List<Asset>();
    }
}   
