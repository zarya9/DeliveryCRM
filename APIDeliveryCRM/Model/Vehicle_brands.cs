using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class VehicleBrand
    {
        [Key]
        public int ID_Brand { get; set; }
        public string Name { get; set; }
    }
}
