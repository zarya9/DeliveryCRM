using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class VehicleBodyType
    {
        [Key]
        public int ID_BodyType { get; set; }
        public string Name { get; set; }
    }
}
