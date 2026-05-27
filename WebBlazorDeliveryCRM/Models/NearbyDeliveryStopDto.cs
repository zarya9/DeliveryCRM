namespace WebBlazorDeliveryCRM.Models;

public class NearbyDeliveryStopDto
{
    public int AssignmentId { get; set; }
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public string StopKind { get; set; } = "Delivery";
}
