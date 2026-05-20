namespace APIDeliveryCRM.Responses;

public class ShiftClosureSummaryDto
{
    public int ShiftId { get; set; }
    public int CourierProfileId { get; set; }
    public string CourierName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime? TimeEnd { get; set; }
    public int? ShiftPlanId { get; set; }
    public int PlanVersion { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public decimal EstimatedFuelLiters { get; set; }
    public decimal EstimatedFuelCostRub { get; set; }
    public int OrdersCompletedCount { get; set; }
    public int RouteStopsCompletedCount { get; set; }
    public List<CourierRouteMapWaypointDto> Waypoints { get; set; } = new();
}
