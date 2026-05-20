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
[Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
public class MonitoringController : Controller
{
    private static readonly TimeSpan LiveLocationFreshness = TimeSpan.FromSeconds(45);
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
            return Unauthorized(new { message = "Не указана компания в токене." });

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
                x.e.NewStatus_id,
                x.e.Created_at
            })
            .ToListAsync();

        var statusIds = rows
            .Where(r => r.NewStatus_id.HasValue)
            .Select(r => r.NewStatus_id!.Value)
            .Distinct()
            .ToList();
        var statusNames = statusIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.OrderStatuses.AsNoTracking()
                .Where(s => statusIds.Contains(s.ID_OrderStatus))
                .ToDictionaryAsync(s => s.ID_OrderStatus, s => s.Name);

        var result = rows.Select(r => new
        {
            r.ID_OrderTimelineEvent,
            r.Order_id,
            r.orderNumber,
            r.EventType,
            r.Title,
            message = FormatFeedMessage(r.EventType, r.Message, r.NewStatus_id, statusNames),
            r.Created_at
        }).ToList();

        return Ok(result);
    }

    [HttpGet("live-map")]
    public async Task<IActionResult> GetLiveMap()
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

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

        var couriers = await _db.CourierProfiles.AsNoTracking()
            .Where(c => c.Company_id == companyId.Value)
            .Include(c => c.User)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var courierMarkers = couriers.Select(c =>
        {
            var started = shiftsByCourier.FirstOrDefault(x => x.CourierId == c.ID_CourierProfile);
            var hasActiveShift = started is not null;
            var hasCoords = c.Current_lat != 0 || c.Current_lon != 0;
            var isFresh = now - c.LastActivity_at <= LiveLocationFreshness;

            if (!c.Is_online || !hasActiveShift || !hasCoords || !isFresh)
                return null;

            return new
            {
                kind = "courier",
                id = c.ID_CourierProfile,
                lat = (double)c.Current_lat,
                lon = (double)c.Current_lon,
                online = c.Is_online,
                title = $"{c.User.FName} {c.User.Name}".Trim() + $" · онлайн · смена с {started!.StartedAt:dd.MM HH:mm} UTC"
            };
        })
        .Where(x => x != null)
        .ToList();

        var hubMarkers = hubs
            .Where(h => h.Address?.Latitude != null && h.Address.Longitude != null && h.Address.Latitude != 0 && h.Address.Longitude != 0)
            .Select(h => new
            {
                kind = "hub",
                id = h.ID_LogisticsHub,
                lat = (double)h.Address!.Latitude!.Value,
                lon = (double)h.Address.Longitude!.Value,
                title = string.IsNullOrWhiteSpace(h.Name) ? "Склад" : h.Name!
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
                        title = $"Склад (из заказа): {order.OriginHub.Name}"
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
                        title = $"Склад (из заказа): {order.DestinationHub.Name}"
                    });
                }
            }
        }

        return Ok(new { couriers = courierMarkers, hubs = hubMarkers });
    }

    private static string? FormatFeedMessage(
        string? eventType,
        string? message,
        int? newStatusId,
        IReadOnlyDictionary<int, string> statusNames)
    {
        if (string.Equals(eventType, "STATUS_CHANGED", StringComparison.OrdinalIgnoreCase))
        {
            if (newStatusId is > 0 && statusNames.TryGetValue(newStatusId.Value, out var name)
                && !string.IsNullOrWhiteSpace(name))
                return $"Статус заказа изменен: {name.Trim()}";

            if (!string.IsNullOrWhiteSpace(message)
                && message.StartsWith("Статус заказа изменен:", StringComparison.OrdinalIgnoreCase))
            {
                var arrow = message.IndexOf("->", StringComparison.Ordinal);
                if (arrow >= 0)
                {
                    var tail = message[(arrow + 2)..].Trim();
                    if (int.TryParse(tail, out var parsedId)
                        && statusNames.TryGetValue(parsedId, out var legacyName)
                        && !string.IsNullOrWhiteSpace(legacyName))
                        return $"Статус заказа изменен: {legacyName.Trim()}";
                }
            }
        }

        return message;
    }

    private int? GetCompanyId()
    {
        var v = User.FindFirst("companyId")?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
