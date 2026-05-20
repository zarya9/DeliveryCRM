namespace WebBlazorDeliveryCRM.Models;

public sealed class CourierRouteMapDto
{
    public int CourierId { get; set; }
    public string CourierName { get; set; } = string.Empty;
    public List<CourierRouteMapMarkerDto> Markers { get; set; } = new();
    public List<CourierRouteMapWaypointDto> Waypoints { get; set; } = new();
}

public sealed class CourierRouteMapMarkerDto
{
    public string Kind { get; set; } = string.Empty;
    public int Id { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? Title { get; set; }
}

public sealed class CourierRouteMapWaypointDto
{
    public int Sequence { get; set; }
    public int OrderId { get; set; }
    public string? Title { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
}
