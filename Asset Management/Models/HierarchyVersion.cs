using System.ComponentModel.DataAnnotations;

namespace Asset_Management.Models
{
    public class HierarchyVersion
    {
        [Key]
        public int Id   { get; set; }

        public DateTime EditedTime { get; set; } = DateTime.UtcNow;

        public string SnapshotJson  { get; set; }

        //What action lead to versoning of Hierarchy    
        public string Action { get; set; }




    }
}
