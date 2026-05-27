using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class ServiceAreaZoneService : IServiceAreaZoneService
    {
        private readonly ContextDB _context;

        public ServiceAreaZoneService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ServiceAreaZone>> GetByCompanyAsync(int companyId)
        {
            return await _context.ServiceAreaZones
                .AsNoTracking()
                .Where(z => z.Company_id == companyId)
                .Include(z => z.Couriers)
                .OrderBy(z => z.Name)
                .ToListAsync();
        }

        public async Task<ServiceAreaZone> CreateAsync(int companyId, CreateServiceAreaZoneRequest request)
        {
            var zone = new ServiceAreaZone
            {
                Company_id = companyId,
                Name = request.Name.Trim(),
                Center_lat = request.Center_lat,
                Center_lon = request.Center_lon,
                Radius_km = request.Radius_km,
                Is_active = true
            };

            _context.ServiceAreaZones.Add(zone);
            await _context.SaveChangesAsync();
            return zone;
        }

        public async Task<ServiceAreaZone?> UpdateAsync(int zoneId, int companyId, UpdateServiceAreaZoneRequest request)
        {
            var zone = await _context.ServiceAreaZones
                .Include(z => z.Couriers)
                .FirstOrDefaultAsync(z => z.ID_ServiceAreaZone == zoneId && z.Company_id == companyId);
            if (zone == null)
                return null;

            zone.Name = request.Name.Trim();
            zone.Center_lat = request.Center_lat;
            zone.Center_lon = request.Center_lon;
            zone.Radius_km = request.Radius_km;
            zone.Is_active = request.Is_active;

            await _context.SaveChangesAsync();
            return zone;
        }

        public async Task<bool> DeleteAsync(int zoneId, int companyId)
        {
            var zone = await _context.ServiceAreaZones
                .FirstOrDefaultAsync(z => z.ID_ServiceAreaZone == zoneId && z.Company_id == companyId);
            if (zone == null)
                return false;

            _context.ServiceAreaZones.Remove(zone);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignCourierAsync(int zoneId, int courierProfileId, int companyId)
        {
            var zone = await _context.ServiceAreaZones
                .FirstOrDefaultAsync(z => z.ID_ServiceAreaZone == zoneId && z.Company_id == companyId);
            if (zone == null)
                return false;

            var courier = await _context.CourierProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == companyId);
            if (courier == null)
                return false;

            var exists = await _context.ServiceAreaZoneCouriers
                .AnyAsync(x => x.ServiceAreaZone_id == zoneId && x.CourierProfile_id == courierProfileId);
            if (exists)
                return true;

            _context.ServiceAreaZoneCouriers.Add(new ServiceAreaZoneCourier
            {
                ServiceAreaZone_id = zoneId,
                CourierProfile_id = courierProfileId
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignCourierAsync(int zoneId, int courierProfileId, int companyId)
        {
            // Проверяем, что зона принадлежит компании
            var zoneExists = await _context.ServiceAreaZones
                .AsNoTracking()
                .AnyAsync(z => z.ID_ServiceAreaZone == zoneId && z.Company_id == companyId);
            if (!zoneExists)
                return false;

            var link = await _context.ServiceAreaZoneCouriers
                .FirstOrDefaultAsync(x => x.ServiceAreaZone_id == zoneId && x.CourierProfile_id == courierProfileId);
            if (link == null)
                return false;

            _context.ServiceAreaZoneCouriers.Remove(link);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
