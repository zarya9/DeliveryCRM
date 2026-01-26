using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class FuelCardStatus
    {
        [Key]
        public int ID_Status { get; set; }
        public string Name { get; set; }
        public bool IsCanBeUsed { get; set; }
    }
}
