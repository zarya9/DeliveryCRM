namespace WebBlazorDeliveryCRM.Models;

public class CompanyPlannerResultDto
{
    public int CompanyId { get; set; }
    public DateTime RunAtUtc { get; set; }
    public string Reason { get; set; } = "manual";
    public List<ShiftPlanSummaryDto> Plans { get; set; } = new();
    public List<UnplannedOrderDto> Unplanned { get; set; } = new();
    public int OnlineCouriers { get; set; }
    public int ActiveShifts { get; set; }
    public int ConsideredOrders { get; set; }
}

public class ShiftPlanSummaryDto
{
    public int ShiftPlanId { get; set; }
    public int CompanyId { get; set; }
    public int ShiftId { get; set; }
    public int CourierId { get; set; }
    public string? CourierName { get; set; }
    public int? VehicleId { get; set; }
    public string? VehicleLabel { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? LastRecomputeReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PlannedStartUtc { get; set; }
    public DateTime? PlannedEndUtc { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public decimal EstimatedDurationMinutes { get; set; }
    public decimal PeakWeightKg { get; set; }
    public decimal PeakVolumeM3 { get; set; }
    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }
    public decimal LoadFactorWeight { get; set; }
    public decimal LoadFactorVolume { get; set; }
    public List<ShiftPlanStopDto> Stops { get; set; } = new();
    public int OrdersCount { get; set; }
    public bool BuiltFromAssignedOrders { get; set; }
    public bool RequiresActiveShift { get; set; }
}

public class ShiftPlanStopDto
{
    public int AssignmentId { get; set; }
    public int Sequence { get; set; }
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public int? OrderRouteStopId { get; set; }
    public string StopKind { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? PlannedStartUtc { get; set; }
    public DateTime? PlannedEndUtc { get; set; }
    public decimal SegmentDistanceKm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? AddressLine { get; set; }
    public int? HubId { get; set; }
    public string? HubName { get; set; }
    public byte Priority { get; set; }
    public DateTime? OrderSlaDueAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class UnplannedOrderDto
{
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public byte Priority { get; set; }
    public string? RouteKind { get; set; }
    public string Reason { get; set; } = string.Empty;
}
