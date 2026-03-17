using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class CourierService : ICourierService
    {
        private readonly ContextDB _context;

        public CourierService(ContextDB context)
        {
            _context = context;
        }

        public async Task<CourierProfile> GetProfileAsync(int courierProfileId)
        {
            return await _context.CourierProfiles
                .Include(c => c.User)
                    .ThenInclude(u => u.Role)
                .Include(c => c.VehicleCategory)
                .Include(c => c.ScheduleType)
                .Include(c => c.CourierStatus)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
        }

        public async Task<CourierProfile?> GetByUserIdAsync(int userId)
        {
            return await _context.CourierProfiles
                .Include(c => c.User)
                .Include(c => c.VehicleCategory)
                .Include(c => c.CourierStatus)
                .FirstOrDefaultAsync(c => c.User_id == userId);
        }

        public async Task<IReadOnlyList<CourierProfile>> GetAllAsync(int? companyId = null)
        {
            var query = _context.CourierProfiles
                .Include(c => c.User)
                .Include(c => c.CourierStatus)
                .Include(c => c.VehicleCategory)
                .Include(c => c.ScheduleType)
                .AsQueryable();
            if (companyId.HasValue)
                query = query.Where(c => c.Company_id == companyId.Value);
            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetActiveOrdersAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .ToListAsync();
        }

        public async Task UpdateLocationAsync(int courierProfileId, decimal lat, decimal lon)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
            {
                return;
            }

            courier.Current_lat = lat;
            courier.Current_lon = lon;
            courier.LastActivity_at = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SetOnlineStatusAsync(int courierProfileId, bool isOnline)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
            {
                return;
            }

            courier.Is_online = isOnline;
            courier.LastActivity_at = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}


