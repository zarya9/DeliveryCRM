using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class VehicleAssignment
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Vehicle))]
        public int Vehicle_id { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Courier))]
        public int Courier_id { get; set; }
        public CourierProfile Courier {  get; set; } = null!;
        public DateTime Start_date { get; set; }
        public DateTime End_date { get; set; }
        public int Mileage_start {  get; set; } //пробег
        public int Mileage_end { get; set; }

    }
}
