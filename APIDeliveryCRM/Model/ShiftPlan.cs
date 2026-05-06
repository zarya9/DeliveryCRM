using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model;

public class ShiftPlan
{
    [Key]
    public int ID_ShiftPlan { get; set; }

    [Required]
    [ForeignKey(nameof(Company))]
    public int Company_id { get; set; }
    public Company Company { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(CourierShift))]
    public int Shift_id { get; set; }
    public CourierShift CourierShift { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(CourierProfile))]
    public int Courier_id { get; set; }
    public CourierProfile CourierProfile { get; set; } = null!;

    [ForeignKey(nameof(Vehicle))]
    public int? Vehicle_id { get; set; }
    public Vehicle? Vehicle { get; set; }

    public ShiftPlanStatus Status { get; set; } = ShiftPlanStatus.Draft;

    public DateTime Created_at { get; set; } = DateTime.UtcNow;
    public DateTime? Activated_at { get; set; }
    public DateTime? Completed_at { get; set; }

    public DateTime? Planned_start_utc { get; set; }
    public DateTime? Planned_end_utc { get; set; }

    public decimal Total_distance_km { get; set; }
    public decimal Estimated_duration_minutes { get; set; }

    public decimal Peak_weight_kg { get; set; }

    public decimal Peak_volume_m3 { get; set; }

    public int Version { get; set; } = 1;

    [MaxLength(200)]
    public string? Last_recompute_reason { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}
