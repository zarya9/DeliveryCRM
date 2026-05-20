namespace APIDeliveryCRM.Responses;

public class CourierRouteMapWaypointDto
{
    public int Sequence { get; set; }
    public int? OrderId { get; set; }
    public int? AssignmentId { get; set; }
    public string? Title { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
}
