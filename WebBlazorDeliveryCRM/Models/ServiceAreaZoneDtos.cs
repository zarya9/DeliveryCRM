namespace WebBlazorDeliveryCRM.Models;

public class ServiceAreaZoneDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CenterLat { get; set; }
    public decimal CenterLon { get; set; }
    public decimal RadiusKm { get; set; }
    public bool IsActive { get; set; }
    public List<int> CourierIds { get; set; } = new();
}

public class CreateServiceAreaZoneRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Center_lat { get; set; }
    public decimal Center_lon { get; set; }
    public decimal Radius_km { get; set; }
}
