using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class CourierFuelCard
    {
        [Key]
        public int ID_CF {  get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(CourierProfile))]
        public int  Courier_id { get; set; }
        public CourierProfile CourierProfile { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(FuelCard))]
        public int FuelCard_id { get; set; }
        public FuelCard FuelCard { get; set; }
        public bool Is_primary { get; set; }
        public bool Is_backup { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int AssignedByUser_id { get; set; }
        public User User { get; set; }

    }
}
