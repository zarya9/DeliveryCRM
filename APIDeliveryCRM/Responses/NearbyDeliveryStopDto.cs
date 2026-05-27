namespace APIDeliveryCRM.Responses;

public class NearbyDeliveryStopDto
{
    public int AssignmentId { get; set; }
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    /// <summary>Pickup — у точки забора; Delivery — у точки доставки.</summary>
    public string StopKind { get; set; } = "Delivery";
}
