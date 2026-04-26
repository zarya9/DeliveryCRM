using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class CompanySettingsService : ICompanySettingsService
{
    private readonly ContextDB _context;

    public CompanySettingsService(ContextDB context)
    {
        _context = context;
    }

    public async Task<IActionResult> GetSlaSettingsAsync(int companyId)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID_Company == companyId);
        if (company == null)
            return new NotFoundObjectResult(new { message = "Компания не найдена." });

        return new OkObjectResult(new CompanySlaSettingsResponse
        {
            CompanyId = company.ID_Company,
            SlaOnTimeHours = company.SlaOnTimeHours,
            SlaLateHours = company.SlaLateHours
        });
    }

    public async Task<IActionResult> UpdateSlaSettingsAsync(int companyId, UpdateCompanySlaSettingsRequest request)
    {
        if (request.SlaOnTimeHours <= 0 || request.SlaOnTimeHours > 168)
            return new BadRequestObjectResult(new { message = "SlaOnTimeHours должен быть в диапазоне 1..168." });
        if (request.SlaLateHours <= 0 || request.SlaLateHours > 720)
            return new BadRequestObjectResult(new { message = "SlaLateHours должен быть в диапазоне 1..720." });
        if (request.SlaLateHours < request.SlaOnTimeHours)
            return new BadRequestObjectResult(new { message = "SlaLateHours не может быть меньше SlaOnTimeHours." });

        var company = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == companyId);
        if (company == null)
            return new NotFoundObjectResult(new { message = "Компания не найдена." });

        company.SlaOnTimeHours = request.SlaOnTimeHours;
        company.SlaLateHours = request.SlaLateHours;
        await _context.SaveChangesAsync();

        return new OkObjectResult(new CompanySlaSettingsResponse
        {
            CompanyId = company.ID_Company,
            SlaOnTimeHours = company.SlaOnTimeHours,
            SlaLateHours = company.SlaLateHours
        });
    }
}

