using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ShiftAssignment
    {
        [Key]
        public int ID_ShiftAssignment { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Shift))]
        public int Shift_id { get; set; }
        public CourierShift Shift { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Order))]
        public int Order_id { get; set; }
        public Order Order { get; set; }

        public int Assignment_sequence { get; set; }

    }
}
