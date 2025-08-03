namespace Asset_Management.Models
{
    public class Asset
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public List<Asset> Children { get; set; } = new List<Asset>();
    }
}
