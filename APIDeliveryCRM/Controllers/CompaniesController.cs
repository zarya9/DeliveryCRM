using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Клиент")]
public class CompaniesController : Controller
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>Службы доставки, в которые клиент может направить новый заказ (активная подписка + компания учётной записи).</summary>
    [HttpGet("for-customer-order")]
    public async Task<IActionResult> GetForCustomerOrder()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var (list, clientMissing) = await _companyService.GetCompaniesForCustomerOrderAsync(userId.Value);
        if (clientMissing)
            return BadRequest(new { message = "Профиль клиента не найден." });

        return Ok(list);
    }

    private int? GetCurrentUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }
}
