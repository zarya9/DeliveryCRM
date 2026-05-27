using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class CompanyService : ICompanyService
{
    private readonly ContextDB _context;

    public CompanyService(ContextDB context)
    {
        _context = context;
    }

    public async Task<bool> HasActiveSubscriptionAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID_Company == companyId, cancellationToken);
        if (company == null || !company.Is_Active)
            return false;
        return company.SubscriptionExpiresAt == default || company.SubscriptionExpiresAt >= DateTime.UtcNow;
    }

    public async Task<(IReadOnlyList<CompanyForCustomerOrderDto> List, bool ClientMissing)> GetCompaniesForCustomerOrderAsync(int userId, CancellationToken cancellationToken = default)
    {
        var client = await _context.ClientProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.User_id == userId, cancellationToken);
        if (client == null)
            return (Array.Empty<CompanyForCustomerOrderDto>(), true);

        const int excludedFromCustomerOrderChoiceCompanyId = 1;

        var now = DateTime.UtcNow;
        var activeCompanyIds = await _context.Companies.AsNoTracking()
            .Where(c => c.ID_Company != excludedFromCustomerOrderChoiceCompanyId)
            .Where(c => c.Is_Active && (c.SubscriptionExpiresAt == default || c.SubscriptionExpiresAt >= now))
            .Select(c => c.ID_Company)
            .ToListAsync(cancellationToken);

        var list = await _context.Companies.AsNoTracking()
            .Where(c => c.ID_Company != excludedFromCustomerOrderChoiceCompanyId)
            .Where(c => activeCompanyIds.Contains(c.ID_Company) || c.ID_Company == client.Company_id)
            .OrderBy(c => c.ID_Company == client.Company_id ? 0 : 1)
            .ThenBy(c => c.Name)
            .Select(c => new CompanyForCustomerOrderDto { Id = c.ID_Company, Name = c.Name })
            .ToListAsync(cancellationToken);

        return (list, false);
    }

    public async Task<CompanyDto?> GetByIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var c = await _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID_Company == companyId, cancellationToken);
        return c == null ? null : MapToDto(c);
    }

    public async Task<IReadOnlyList<CompanyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _context.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<CompanyDto> UpdateAsync(int companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.ID_Company == companyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Компания {companyId} не найдена.");

        company.Name = request.Name.Trim();
        company.Subdomain = string.IsNullOrWhiteSpace(request.Subdomain) ? null : request.Subdomain.Trim();
        company.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        company.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? null : request.PrimaryColor.Trim();
        company.SecondaryColor = string.IsNullOrWhiteSpace(request.SecondaryColor) ? null : request.SecondaryColor.Trim();

        if (!string.IsNullOrWhiteSpace(request.SubscriptionPlan))
            company.SubscriptionPlan = request.SubscriptionPlan.Trim();
        if (request.MaxUsers.HasValue)
            company.MaxUsers = request.MaxUsers.Value;
        if (request.MaxOrdersPerMonth.HasValue)
            company.MaxOrdersPerMonth = request.MaxOrdersPerMonth.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(company);
    }

    public async Task<bool> SetActiveAsync(int companyId, bool isActive, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.ID_Company == companyId, cancellationToken);
        if (company == null)
            return false;

        company.Is_Active = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CompanyDto MapToDto(Model.Company c) => new()
    {
        Id = c.ID_Company,
        Name = c.Name,
        Subdomain = c.Subdomain,
        LogoUrl = c.LogoUrl,
        PrimaryColor = c.PrimaryColor,
        SecondaryColor = c.SecondaryColor,
        IsActive = c.Is_Active,
        SubscriptionPlan = c.SubscriptionPlan,
        MaxUsers = c.MaxUsers,
        MaxOrdersPerMonth = c.MaxOrdersPerMonth,
        SubscriptionExpiresAt = c.SubscriptionExpiresAt,
        SlaOnTimeHours = c.SlaOnTimeHours,
        SlaLateHours = c.SlaLateHours,
        CreatedAt = c.Created_at
    };
}
