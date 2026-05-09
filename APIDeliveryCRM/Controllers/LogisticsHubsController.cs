using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Helpers;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogisticsHubsController : Controller
{
    private readonly ILogisticsHubService _hubService;
    private readonly ContextDB _db;

    public LogisticsHubsController(ILogisticsHubService hubService, ContextDB db)
    {
        _hubService = hubService;
        _db = db;
    }

    [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var list = await _hubService.GetByCompanyAsync(companyId.Value);
        var openOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.Company_id == companyId.Value && o.Delivered_at == null)
            .Include(o => o.ClientProfile).ThenInclude(c => c.User)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.RouteStops)
            .ToListAsync();

        return Ok(list.Select(h =>
        {
            var onSite = openOrders
                .Where(o => HubOccupancyHelper.IsOrderAtHub(o, h.ID_LogisticsHub))
                .Select(o => new
                {
                    orderId = o.ID_Order,
                    orderNumber = o.Order_Number,
                    clientName = HubOccupancyHelper.FormatClientName(o.ClientProfile),
                    deliveryTo = HubOccupancyHelper.FormatDeliveryLine(o.DeliveryAddress)
                })
                .ToList();

            return new
            {
                id = h.ID_LogisticsHub,
                name = h.Name,
                addressId = h.Address_id,
                city = h.Address?.City,
                street = h.Address?.Street,
                house = h.Address?.House,
                flat = h.Address?.Flat,
                region = h.Address?.Region,
                postalCode = h.Address?.PostalCode,
                latitude = h.Address?.Latitude,
                longitude = h.Address?.Longitude,
                ordersOnSite = onSite
            };
        }));
    }

    [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogisticsHubRequest request)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(new { message = "Не указан пользователь в токене." });

        var hub = await _hubService.CreateAsync(companyId.Value, userId.Value, request);
        return Ok(new { id = hub.ID_LogisticsHub });
    }

    [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateLogisticsHubRequest request)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var updated = await _hubService.UpdateAsync(companyId.Value, id, request);
        if (updated == null)
            return NotFound();

        return Ok(new { id = updated.ID_LogisticsHub });
    }

    [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var (ok, error) = await _hubService.DeleteAsync(companyId.Value, id);
        if (!ok)
        {
            if (!string.IsNullOrWhiteSpace(error))
                return BadRequest(new { message = error });
            return NotFound();
        }

        return Ok(new { deleted = true });
    }

    private int? GetCompanyId()
    {
        var v = User.FindFirst("companyId")?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
