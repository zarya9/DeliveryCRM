using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogisticsHubsController : ControllerBase
{
    private readonly ILogisticsHubService _hubService;

    public LogisticsHubsController(ILogisticsHubService hubService)
    {
        _hubService = hubService;
    }

    [Authorize(Roles = "Логист,Админ,Менеджер")]
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Не указана компания в токене." });

        var list = await _hubService.GetByCompanyAsync(companyId.Value);
        return Ok(list.Select(h => new
        {
            id = h.ID_LogisticsHub,
            name = h.Name,
            addressId = h.Address_id,
            city = h.Address?.City,
            street = h.Address != null ? $"{h.Address.Street}, {h.Address.House}" : null
        }));
    }

    [Authorize(Roles = "Логист,Админ,Менеджер")]
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
