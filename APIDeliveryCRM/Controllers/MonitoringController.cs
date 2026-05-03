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
                x.e.Created_at
            })
            .ToListAsync();

        return Ok(rows);
    }

    /// <summary>Курьеры на активной смене с координатами и склады компании — для карты мониторинга.</summary>
    [HttpGet("live-map")]
    public async Task<IActionResult> GetLiveMap()
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var activeShiftCourierIds = await _db.CourierShifts.AsNoTracking()
            .Where(s => s.Company_id == companyId.Value && s.TimeEnd == null)
            .Select(s => s.Courier_id)
            .Distinct()
            .ToListAsync();

        var shiftsByCourier = await _db.CourierShifts.AsNoTracking()
            .Where(s => s.Company_id == companyId.Value && s.TimeEnd == null)
            .GroupBy(s => s.Courier_id)
            .Select(g => new { CourierId = g.Key, StartedAt = g.Max(x => x.TimeStart) })
            .ToListAsync();

        var couriers = await _db.CourierProfiles.AsNoTracking()
            .Where(c => c.Company_id == companyId.Value && activeShiftCourierIds.Contains(c.ID_CourierProfile))
            .Include(c => c.User)
            .ToListAsync();

        var courierMarkers = couriers.Select(c =>
        {
            var started = shiftsByCourier.FirstOrDefault(x => x.CourierId == c.ID_CourierProfile)?.StartedAt;
            return new
            {
                kind = "courier",
                id = c.ID_CourierProfile,
                lat = (double)c.Current_lat,
                lon = (double)c.Current_lon,
                title = $"{c.User.FName} {c.User.Name}".Trim() + (started.HasValue
                    ? $" · смена с {started.Value:dd.MM HH:mm} UTC"
                    : "")
            };
        }).ToList();

        var hubs = await _db.LogisticsHubs.AsNoTracking()
            .Where(h => h.Company_id == companyId.Value)
            .Include(h => h.Address)
            .OrderBy(h => h.Name)
            .ToListAsync();

        var hubMarkers = hubs
            .Where(h => h.Address?.Latitude != null && h.Address.Longitude != null)
            .Select(h => new
            {
                kind = "hub",
                id = h.ID_LogisticsHub,
                lat = (double)h.Address!.Latitude!.Value,
                lon = (double)h.Address.Longitude!.Value,
                title = string.IsNullOrWhiteSpace(h.Name) ? "Склад" : h.Name!
            })
            .ToList();

        return Ok(new { couriers = courierMarkers, hubs = hubMarkers });
    }

    private int? GetCompanyId()
    {
        var v = User.FindFirst("companyId")?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
