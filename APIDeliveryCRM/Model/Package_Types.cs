using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class PackageType
    {
        [Key]
        public int ID_PackageType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Max_weight { get; set; }
        public decimal Max_height { get; set; }
        public decimal Max_wight { get; set; }
        public decimal Max_length { get; set; }
    }
}
