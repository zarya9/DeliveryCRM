using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class LogisticsHubService : ILogisticsHubService
{
    private readonly ContextDB _context;

    public LogisticsHubService(ContextDB context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LogisticsHub>> GetByCompanyAsync(int companyId)
    {
        return await _context.LogisticsHubs
            .AsNoTracking()
            .Where(h => h.Company_id == companyId)
            .Include(h => h.Address)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<LogisticsHub> CreateAsync(int companyId, int userId, CreateLogisticsHubRequest request)
    {
        var address = new Address
        {
            Street = request.Street,
            House = request.House,
            Flat = request.Flat,
            City = request.City,
            Region = request.Region,
            PostalCode = request.PostalCode,
            Comment = request.Comment,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Company_id = companyId,
            User_id = userId
        };
        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        var hub = new LogisticsHub
        {
            Company_id = companyId,
            Name = request.Name.Trim(),
            Address_id = address.ID_Address
        };
        _context.LogisticsHubs.Add(hub);
        await _context.SaveChangesAsync();

        return await _context.LogisticsHubs.Include(h => h.Address)
            .FirstAsync(h => h.ID_LogisticsHub == hub.ID_LogisticsHub);
    }
}
