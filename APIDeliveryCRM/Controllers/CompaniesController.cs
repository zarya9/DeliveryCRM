using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Клиент")]
public class CompaniesController : Controller
{
    private readonly ContextDB _context;

    public CompaniesController(ContextDB context)
    {
        _context = context;
    }

    /// <summary>Службы доставки, в которые клиент может направить новый заказ (активная подписка + компания учётной записи).</summary>
    [HttpGet("for-customer-order")]
    public async Task<IActionResult> GetForCustomerOrder()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var client = await _context.ClientProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.User_id == userId.Value);
        if (client == null)
            return BadRequest(new { message = "Профиль клиента не найден." });

        var now = DateTime.UtcNow;
        var activeCompanyIds = await _context.Companies.AsNoTracking()
            .Where(c => c.Is_Active && (c.SubscriptionExpiresAt == default || c.SubscriptionExpiresAt >= now))
            .Select(c => c.ID_Company)
            .ToListAsync();

        var list = await _context.Companies.AsNoTracking()
            .Where(c => activeCompanyIds.Contains(c.ID_Company) || c.ID_Company == client.Company_id)
            .OrderBy(c => c.ID_Company == client.Company_id ? 0 : 1)
            .ThenBy(c => c.Name)
            .Select(c => new { id = c.ID_Company, name = c.Name })
            .ToListAsync();

        return Ok(list);
    }

    private int? GetCurrentUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
