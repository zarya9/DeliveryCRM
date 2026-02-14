using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Interfaces
{
    public interface ICourierService
    {
        Task<CourierProfile> GetProfileAsync(int courierProfileId);
        Task<CourierProfile?> GetByUserIdAsync(int userId);
        Task<IReadOnlyList<CourierProfile>> GetAllAsync(int? companyId = null);
        Task<IReadOnlyList<Order>> GetActiveOrdersAsync(int courierProfileId);
        Task UpdateLocationAsync(int courierProfileId, decimal lat, decimal lon);
        Task SetOnlineStatusAsync(int courierProfileId, bool isOnline);
    }
}


