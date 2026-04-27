namespace WebBlazorDeliveryCRM.Models;

public class GeoAnalyticsOverviewDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalOrders { get; set; }
    public int GeoOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int LateDeliveredOrders { get; set; }
    public double LatePercent { get; set; }
    public double? AvgDeliveryHours { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgCheck { get; set; }
    public int ActiveCouriers { get; set; }
    public List<GeoPointDto> HeatPoints { get; set; } = new();
    public List<GeoZonePerformanceDto> ZonePerformance { get; set; } = new();
    public List<GeoCourierPerformanceDto> CourierPerformance { get; set; } = new();
    public List<GeoHourlyDemandDto> HourlyDemand { get; set; } = new();
    public List<GeoStatusDistributionDto> StatusDistribution { get; set; } = new();
    public List<GeoZoneCircleDto> Zones { get; set; } = new();
}

public class GeoPointDto
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double Intensity { get; set; }
    public int Orders { get; set; }
}

public class GeoZonePerformanceDto
{
    public string ZoneName { get; set; } = string.Empty;
    public int Orders { get; set; }
    public int Delivered { get; set; }
    public double? AvgDeliveryHours { get; set; }
    public double LatePercent { get; set; }
    public decimal Revenue { get; set; }
}

public class GeoCourierPerformanceDto
{
    public int CourierId { get; set; }
    public string CourierName { get; set; } = string.Empty;
    public int Orders { get; set; }
    public int Delivered { get; set; }
    public double? AvgDeliveryHours { get; set; }
    public double LatePercent { get; set; }
}

public class GeoHourlyDemandDto
{
    public int Hour { get; set; }
    public int Orders { get; set; }
}

public class GeoStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class GeoZoneCircleDto
{
    public int ZoneId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public double RadiusKm { get; set; }
    public bool IsActive { get; set; }
}
