using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class FuelCardType
    {
        [Key]
        public int ID_Type { get; set; }
        public string Name { get; set; }
        public string Priority { get; set; }
    }
}
