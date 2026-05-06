using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Р›РѕРіРёСЃС‚,РђРґРјРёРЅРёСЃС‚СЂР°С‚РѕСЂ,РђРґРјРёРЅ,РњРµРЅРµРґР¶РµСЂ")]
public class MonitoringController : Controller
{
    private readonly ContextDB _db;

    public MonitoringController(ContextDB db)
    {
        _db = db;
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int hours = 48, [FromQuery] int take = 80)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "РќРµ СѓРєР°Р·Р°РЅР° РєРѕРјРїР°РЅРёСЏ РІ С‚РѕРєРµРЅРµ." });

        take = Math.Clamp(take, 10, 200);
        hours = Math.Clamp(hours, 1, 168);
        var from = DateTime.UtcNow.AddHours(-hours);

        var rows = await _db.OrderTimelineEvents.AsNoTracking()
            .Join(_db.Orders.AsNoTracking(), e => e.Order_id, o => o.ID_Order, (e, o) => new { e, o })
            .Where(x => x.o.Company_id == companyId.Value && x.e.Created_at >= from)
            .OrderByDescending(x => x.e.Created_at)
            .Take(take)
            .Select(x => new
            {
                x.e.ID_OrderTimelineEvent,
                x.e.Order_id,
                orderNumber = x.o.Order_Number,
                x.e.EventType,
                x.e.Title,
                x.e.Message,
                x.e.Created_at
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("live-map")]
    public async Task<IActionResult> GetLiveMap()
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "РќРµ СѓРєР°Р·Р°РЅР° РєРѕРјРїР°РЅРёСЏ РІ С‚РѕРєРµРЅРµ." });

        var shiftsByCourier = await _db.CourierShifts.AsNoTracking()
            .Where(s => s.Company_id == companyId.Value && s.TimeEnd == null)
            .GroupBy(s => s.Courier_id)
            .Select(g => new { CourierId = g.Key, StartedAt = g.Max(x => x.TimeStart) })
            .ToListAsync();

        var hubs = await _db.LogisticsHubs.AsNoTracking()
            .Where(h => h.Company_id == companyId.Value)
            .Include(h => h.Address)
            .OrderBy(h => h.Name)
            .ToListAsync();

        var fallbackHub = hubs.FirstOrDefault(h =>
            h.Address?.Latitude is not null && h.Address.Longitude is not null &&
            h.Address.Latitude != 0 && h.Address.Longitude != 0);

        var couriers = await _db.CourierProfiles.AsNoTracking()
            .Where(c => c.Company_id == companyId.Value)
            .Include(c => c.User)
            .ToListAsync();

        var courierMarkers = couriers.Select(c =>
        {
            var started = shiftsByCourier.FirstOrDefault(x => x.CourierId == c.ID_CourierProfile)?.StartedAt;
            var onlineText = c.Is_online ? "РѕРЅР»Р°Р№РЅ" : "РѕС„Р»Р°Р№РЅ";
            var hasCoords = c.Current_lat != 0 || c.Current_lon != 0;
            var markerLat = hasCoords
                ? (double)c.Current_lat
                : (double?)(fallbackHub?.Address?.Latitude ?? 0m) ?? 0d;
            var markerLon = hasCoords
                ? (double)c.Current_lon
                : (double?)(fallbackHub?.Address?.Longitude ?? 0m) ?? 0d;
            var fallbackSuffix = !hasCoords && c.Is_online && markerLat != 0d && markerLon != 0d
                ? " В· РєРѕРѕСЂРґРёРЅР°С‚С‹ СѓС‚РѕС‡РЅСЏСЋС‚СЃСЏ"
                : string.Empty;

            return new
            {
                kind = "courier",
                id = c.ID_CourierProfile,
                lat = markerLat,
                lon = markerLon,
                online = c.Is_online,
                title = $"{c.User.FName} {c.User.Name}".Trim() + $" В· {onlineText}" + (started.HasValue
                    ? $" В· СЃРјРµРЅР° СЃ {started.Value:dd.MM HH:mm} UTC"
                    : "") + fallbackSuffix
            };
        }).ToList();

        var hubMarkers = hubs
            .Where(h => h.Address?.Latitude != null && h.Address.Longitude != null && h.Address.Latitude != 0 && h.Address.Longitude != 0)
            .Select(h => new
            {
                kind = "hub",
                id = h.ID_LogisticsHub,
                lat = (double)h.Address!.Latitude!.Value,
                lon = (double)h.Address.Longitude!.Value,
                title = string.IsNullOrWhiteSpace(h.Name) ? "РЎРєР»Р°Рґ" : h.Name!
            })
            .ToList();

        if (hubMarkers.Count == 0)
        {
            var orderHubFallback = await _db.Orders.AsNoTracking()
                .Where(o => o.Company_id == companyId.Value)
                .Include(o => o.OriginHub)
                    .ThenInclude(h => h!.Address)
                .Include(o => o.DestinationHub)
                    .ThenInclude(h => h!.Address)
                .OrderByDescending(o => o.Created_at)
                .Take(50)
                .ToListAsync();

            foreach (var order in orderHubFallback)
            {
                if (order.OriginHub?.Address?.Latitude is { } olat && order.OriginHub.Address.Longitude is { } olon && olat != 0 && olon != 0)
                {
                    hubMarkers.Add(new
                    {
                        kind = "hub",
                        id = order.OriginHub.ID_LogisticsHub,
                        lat = (double)olat,
                        lon = (double)olon,
                        title = $"РЎРєР»Р°Рґ (РёР· Р·Р°РєР°Р·Р°): {order.OriginHub.Name}"
                    });
                }
                if (order.DestinationHub?.Address?.Latitude is { } dlat && order.DestinationHub.Address.Longitude is { } dlon && dlat != 0 && dlon != 0)
                {
                    hubMarkers.Add(new
                    {
                        kind = "hub",
                        id = order.DestinationHub.ID_LogisticsHub,
                        lat = (double)dlat,
                        lon = (double)dlon,
                        title = $"РЎРєР»Р°Рґ (РёР· Р·Р°РєР°Р·Р°): {order.DestinationHub.Name}"
                    });
                }
            }
        }

        return Ok(new { couriers = courierMarkers, hubs = hubMarkers });
    }

    private int? GetCompanyId()
    {
        var v = User.FindFirst("companyId")?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
