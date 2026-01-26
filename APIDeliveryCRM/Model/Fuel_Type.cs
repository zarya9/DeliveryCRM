using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class FuelType
    {
        [Key]
        public int ID_FuelType { get; set; }
        public string Name { get; set; }
    }
}
