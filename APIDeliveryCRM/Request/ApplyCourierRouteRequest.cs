namespace APIDeliveryCRM.Request;

public class ApplyCourierRouteRequest
{
    public List<ApplyCourierRouteStopRequest> Stops { get; set; } = new();
}

public class ApplyCourierRouteStopRequest
{
    public int Sequence { get; set; }
    public int? OrderId { get; set; }
    public int? OrderRouteStopId { get; set; }
    public string? Title { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
