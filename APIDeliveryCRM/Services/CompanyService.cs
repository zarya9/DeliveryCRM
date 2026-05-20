using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
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
}
