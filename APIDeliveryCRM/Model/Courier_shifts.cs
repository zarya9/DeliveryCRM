using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class CourierShift
    {
        [Key]
        public int ID_Shift { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(CourierProfile))]
        public int Courier_id { get; set; }
        public CourierProfile CourierProfile { get; set; } = null!;
        public DateOnly Date {  get; set; }  
        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }

        [Required]
        [ForeignKey(nameof(ShiftStatus))]
        public int ShiftStatus_id { get; set; }
        public ShiftStatus ShiftStatus { get; set; } = null!;
    }
}
