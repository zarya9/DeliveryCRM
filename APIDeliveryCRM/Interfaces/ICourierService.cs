using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;

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
        Task<IReadOnlyList<Vehicle>> GetVehiclesByCompanyAsync(int companyId);
        Task AssignVehicleAsync(int courierProfileId, int vehicleId, int? actorUserId = null, string? ipAddress = null);
        Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest dto, int companyId, int? actorUserId = null, string? ipAddress = null);
        Task UpdateCourierDocumentsAsync(int courierProfileId, int companyId, string? driverLicense, string? passportData);
        Task EnsureCourierProfileForUserAsync(int userId, int companyId);
    }
}


