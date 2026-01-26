using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class ShiftStatus
    {
        [Key]
        public int ID_ShiftStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public ICollection<CourierShift> CourierShifts { get; set; } = new List<CourierShift>();
    }
}
