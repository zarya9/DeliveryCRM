using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Менеджер,Админ")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("finance")]
    public async Task<IActionResult> GetFinanceDashboard(
        [FromQuery] int? companyId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden)
            return new ForbidResult();
        if (resolvedCompanyId <= 0)
            return new BadRequestObjectResult(new { message = "Не удалось определить компанию." });

        return await _reportService.GetFinanceDashboardAsync(resolvedCompanyId, from, to);
    }

    [HttpGet("finance/export")]
    public async Task<IActionResult> ExportFinanceExcel(
        [FromQuery] int? companyId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden)
            return new ForbidResult();
        if (resolvedCompanyId <= 0)
            return new BadRequestObjectResult(new { message = "Не удалось определить компанию." });

        return await _reportService.ExportFinanceExcelAsync(resolvedCompanyId, from, to);
    }

    private int ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
    {
        forbidden = false;
        var claimCompanyId = GetCompanyIdFromClaims();
        if (!requestedCompanyId.HasValue)
            return claimCompanyId;
        if (IsAdmin())
            return requestedCompanyId.Value;
        if (claimCompanyId <= 0 || claimCompanyId != requestedCompanyId.Value)
        {
            forbidden = true;
            return 0;
        }
        return claimCompanyId;
    }

    private int GetCompanyIdFromClaims()
    {
        var raw = User.FindFirst("companyId")?.Value
                  ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
        return int.TryParse(raw, out var id) ? id : 0;
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        return string.Equals(role, "Админ", StringComparison.OrdinalIgnoreCase);
    }
}
