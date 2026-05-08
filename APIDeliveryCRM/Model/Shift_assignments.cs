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
        public Order Order { get; set; } = null!;

        public int Assignment_sequence { get; set; }

        [ForeignKey(nameof(ShiftPlan))]
        public int? ShiftPlan_id { get; set; }
        public ShiftPlan? ShiftPlan { get; set; }

        [ForeignKey(nameof(OrderRouteStop))]
        public int? OrderRouteStop_id { get; set; }
        public OrderRouteStop? OrderRouteStop { get; set; }

        public ShiftAssignmentStage Stage { get; set; } = ShiftAssignmentStage.LocalUrban;

        public ShiftAssignmentStatus Status { get; set; } = ShiftAssignmentStatus.Pending;

        public DateTime? Planned_start_utc { get; set; }

        public DateTime? Planned_end_utc { get; set; }

        public decimal Planned_distance_km { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
