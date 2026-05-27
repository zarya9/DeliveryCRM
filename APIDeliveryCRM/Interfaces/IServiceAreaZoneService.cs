using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Interfaces
{
    public interface IServiceAreaZoneService
    {
        Task<IReadOnlyList<ServiceAreaZone>> GetByCompanyAsync(int companyId);
        Task<ServiceAreaZone> CreateAsync(int companyId, CreateServiceAreaZoneRequest request);
        Task<bool> AssignCourierAsync(int zoneId, int courierProfileId, int companyId);
        Task<ServiceAreaZone?> UpdateAsync(int zoneId, int companyId, UpdateServiceAreaZoneRequest request);
        Task<bool> DeleteAsync(int zoneId, int companyId);
        Task<bool> UnassignCourierAsync(int zoneId, int courierProfileId, int companyId);
    }
}
