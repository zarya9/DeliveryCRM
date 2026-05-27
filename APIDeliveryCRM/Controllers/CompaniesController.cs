using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : Controller
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>Службы доставки, в которые клиент может направить новый заказ (активная подписка + компания учётной записи).</summary>
    [HttpGet("for-customer-order")]
    [Authorize(Roles = "Клиент")]
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

    /// <summary>Получить компанию по ID. Администратор — любую, остальные — только свою.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Администратор,Админ,Менеджер,Логист,Курьер")]
    public async Task<IActionResult> GetById(int id)
    {
        var companyId = GetCurrentCompanyId();
        var isAdmin = IsAdmin();

        // Не-администраторы могут видеть только свою компанию
        if (!isAdmin && companyId != id)
            return Forbid();

        var dto = await _companyService.GetByIdAsync(id);
        if (dto == null)
            return NotFound(new { message = "Компания не найдена." });

        return Ok(dto);
    }

    /// <summary>Список всех компаний. Только для суперадминистраторов (companyId=1).</summary>
    [HttpGet]
    [Authorize(Roles = "Администратор,Админ")]
    public async Task<IActionResult> GetAll()
    {
        if (!IsSuperAdmin())
            return Forbid();

        var list = await _companyService.GetAllAsync();
        return Ok(list);
    }

    /// <summary>Обновить данные компании. Администратор своей компании или суперадмин.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Администратор,Админ")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyRequest request)
    {
        var companyId = GetCurrentCompanyId();
        if (!IsSuperAdmin() && companyId != id)
            return Forbid();

        try
        {
            var dto = await _companyService.UpdateAsync(id, request);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Компания не найдена." });
        }
    }

    /// <summary>Активировать / деактивировать компанию. Только суперадмин.</summary>
    [HttpPatch("{id:int}/active")]
    [Authorize(Roles = "Администратор,Админ")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var ok = await _companyService.SetActiveAsync(id, isActive);
        if (!ok)
            return NotFound(new { message = "Компания не найдена." });

        return Ok(new { id, isActive });
    }

    private int? GetCurrentUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }

    private int GetCurrentCompanyId()
    {
        var v = User.FindFirst("companyId")?.Value;
        return int.TryParse(v, out var id) ? id : 0;
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        return string.Equals(role, "Админ", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Администратор", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Суперадмин — администратор компании с ID=1.</summary>
    private bool IsSuperAdmin() => IsAdmin() && GetCurrentCompanyId() == 1;
}
