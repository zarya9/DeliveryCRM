using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Responses;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class GeoAnalyticsService : IGeoAnalyticsService
{
    private const double EarthRadiusKm = 6371.0;
    private readonly ContextDB _context;

    public GeoAnalyticsService(ContextDB context)
    {
        _context = context;
    }

    public async Task<GeoAnalyticsOverviewDto> GetOverviewAsync(int companyId, DateTime fromUtc, DateTime toUtc, double gridKm)
    {
        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);
        var effectiveGridKm = Math.Clamp(gridKm, 0.2, 30.0);
        var cellDeg = effectiveGridKm / 111.0;

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Company_id == companyId && o.Created_at >= from && o.Created_at <= to)
            .Include(o => o.OrderStatus)
            .Include(o => o.PickupAddress)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.CourierProfile)
                .ThenInclude(c => c!.User)
            .ToListAsync();

        var zones = await _context.ServiceAreaZones
            .AsNoTracking()
            .Where(z => z.Company_id == companyId)
            .OrderBy(z => z.Name)
            .ToListAsync();

        var geoOrders = orders.Count(o => HasCoords(o.PickupAddress) || HasCoords(o.DeliveryAddress));
        var delivered = orders.Where(o => o.Delivered_at.HasValue).ToList();
        var lateDelivered = delivered.Count(o => o.Sla_due_at.HasValue && o.Delivered_at!.Value > o.Sla_due_at.Value);
        var avgHours = delivered.Count == 0
            ? (double?)null
            : delivered.Average(o => Math.Max(0, (o.Delivered_at!.Value - o.Created_at).TotalHours));
        var revenue = delivered.Sum(o => o.Final_cost > 0 ? o.Final_cost : o.Estimated_cost);
        var activeCouriers = orders.Where(o => o.CourierProfile != null)
            .Select(o => o.CourierProfile!.ID_CourierProfile)
            .Distinct()
            .Count();

        var heatPoints = BuildHeatPoints(orders, cellDeg);
        var zoneRows = BuildZonePerformance(orders, zones);
        var courierRows = BuildCourierPerformance(orders);
        var hourlyRows = Enumerable.Range(0, 24)
            .Select(hour => new GeoHourlyDemandDto
            {
                Hour = hour,
                Orders = orders.Count(o => o.Created_at.Hour == hour)
            })
            .ToList();
        var statusRows = orders
            .GroupBy(o => o.OrderStatus?.Name ?? "Неизвестно")
            .Select(g => new GeoStatusDistributionDto { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new GeoAnalyticsOverviewDto
        {
            FromUtc = from,
            ToUtc = to,
            TotalOrders = orders.Count,
            GeoOrders = geoOrders,
            DeliveredOrders = delivered.Count,
            LateDeliveredOrders = lateDelivered,
            LatePercent = delivered.Count == 0 ? 0 : Math.Round(100.0 * lateDelivered / delivered.Count, 1),
            AvgDeliveryHours = avgHours is null ? null : Math.Round(avgHours.Value, 2),
            Revenue = revenue,
            AvgCheck = delivered.Count == 0 ? 0 : Math.Round(revenue / delivered.Count, 2),
            ActiveCouriers = activeCouriers,
            HeatPoints = heatPoints,
            ZonePerformance = zoneRows,
            CourierPerformance = courierRows,
            HourlyDemand = hourlyRows,
            StatusDistribution = statusRows,
            Zones = zones.Select(z => new GeoZoneCircleDto
            {
                ZoneId = z.ID_ServiceAreaZone,
                Name = z.Name,
                CenterLat = (double)z.Center_lat,
                CenterLon = (double)z.Center_lon,
                RadiusKm = (double)z.Radius_km,
                IsActive = z.Is_active
            }).ToList()
        };
    }

    private static bool HasCoords(Model.Address? address)
        => address?.Latitude.HasValue == true && address.Longitude.HasValue == true;

    private static List<GeoPointDto> BuildHeatPoints(List<Model.Order> orders, double cellDeg)
    {
        var points = orders
            .SelectMany(o => new[]
            {
                ToGeoSeed(o, o.PickupAddress, 0.7),
                ToGeoSeed(o, o.DeliveryAddress, 1.0)
            })
            .Where(x => x != null)
            .Select(x => x!)
            .GroupBy(x => x.Key)
            .Select(g => new GeoPointDto
            {
                Lat = g.Average(x => x.Lat),
                Lon = g.Average(x => x.Lon),
                Orders = g.Count(),
                Intensity = Math.Min(1.0, 0.25 + g.Count() * 0.08)
            })
            .OrderByDescending(x => x.Orders)
            .Take(600)
            .ToList();

        return points;

        GeoSeed? ToGeoSeed(Model.Order order, Model.Address? address, double weight)
        {
            if (!HasCoords(address))
                return null;
            var lat = (double)address!.Latitude!.Value;
            var lon = (double)address.Longitude!.Value;
            var latCell = Math.Floor(lat / cellDeg) * cellDeg;
            var lonCell = Math.Floor(lon / cellDeg) * cellDeg;
            return new GeoSeed($"{latCell:0.0000}|{lonCell:0.0000}", lat, lon, weight);
        }
    }

    private static List<GeoZonePerformanceDto> BuildZonePerformance(List<Model.Order> orders, List<Model.ServiceAreaZone> zones)
    {
        var rows = zones.Select(zone =>
        {
            var zoneOrders = orders.Where(o =>
            {
                var d = o.DeliveryAddress;
                if (!HasCoords(d)) return false;
                var dist = HaversineKm((double)d!.Latitude!.Value, (double)d.Longitude!.Value, (double)zone.Center_lat, (double)zone.Center_lon);
                return dist <= (double)zone.Radius_km;
            }).ToList();

            var delivered = zoneOrders.Where(o => o.Delivered_at.HasValue).ToList();
            var late = delivered.Count(o => o.Sla_due_at.HasValue && o.Delivered_at!.Value > o.Sla_due_at.Value);
            var avgHours = delivered.Count == 0 ? (double?)null : delivered.Average(o => (o.Delivered_at!.Value - o.Created_at).TotalHours);
            var revenue = delivered.Sum(o => o.Final_cost > 0 ? o.Final_cost : o.Estimated_cost);

            return new GeoZonePerformanceDto
            {
                ZoneName = zone.Name,
                Orders = zoneOrders.Count,
                Delivered = delivered.Count,
                AvgDeliveryHours = avgHours.HasValue ? Math.Round(avgHours.Value, 2) : null,
                LatePercent = delivered.Count == 0 ? 0 : Math.Round(100.0 * late / delivered.Count, 1),
                Revenue = revenue
            };
        })
        .OrderByDescending(x => x.Orders)
        .Take(20)
        .ToList();

        return rows;
    }

    private static List<GeoCourierPerformanceDto> BuildCourierPerformance(List<Model.Order> orders)
    {
        return orders
            .Where(o => o.CourierProfile != null)
            .GroupBy(o => new
            {
                CourierId = o.CourierProfile!.ID_CourierProfile,
                Name = $"{o.CourierProfile.User.FName} {o.CourierProfile.User.Name}".Trim()
            })
            .Select(g =>
            {
                var delivered = g.Where(x => x.Delivered_at.HasValue).ToList();
                var late = delivered.Count(x => x.Sla_due_at.HasValue && x.Delivered_at!.Value > x.Sla_due_at.Value);
                var avgHours = delivered.Count == 0 ? (double?)null : delivered.Average(x => (x.Delivered_at!.Value - x.Created_at).TotalHours);
                return new GeoCourierPerformanceDto
                {
                    CourierId = g.Key.CourierId,
                    CourierName = string.IsNullOrWhiteSpace(g.Key.Name) ? $"Курьер #{g.Key.CourierId}" : g.Key.Name,
                    Orders = g.Count(),
                    Delivered = delivered.Count,
                    AvgDeliveryHours = avgHours.HasValue ? Math.Round(avgHours.Value, 2) : null,
                    LatePercent = delivered.Count == 0 ? 0 : Math.Round(100.0 * late / delivered.Count, 1)
                };
            })
            .OrderByDescending(x => x.Orders)
            .Take(20)
            .ToList();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRad(double value) => value * Math.PI / 180.0;

    private sealed record GeoSeed(string Key, double Lat, double Lon, double Weight);
}
