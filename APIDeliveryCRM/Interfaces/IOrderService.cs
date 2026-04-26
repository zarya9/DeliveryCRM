using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces
{
    public interface IOrderService
    {
        Task<Order> GetByIdAsync(int id);
        Task<IReadOnlyList<Order>> GetAllAsync(int? companyId = null, DateTime? fromUtc = null, DateTime? toUtc = null);
        Task<IReadOnlyList<Order>> GetByClientAsync(int clientProfileId);
        Task<IReadOnlyList<Order>> GetByCourierAsync(int courierProfileId);
        Task<Order> CreateAsync(CreateOrderRequest request);
        Task<Order> UpdateAsync(Order order);
        Task<bool> ChangeStatusAsync(int orderId, int statusId);
        Task<bool> AssignCourierAsync(int orderId, int courierProfileId);
        Task<OrderDispatchDto?> AutoDispatchAsync(int orderId);
        Task<bool> ManualOverrideCourierAsync(int orderId, int courierProfileId, string? reason, int? actorUserId = null);
        Task<IReadOnlyList<OrderTimelineEvent>> GetTimelineAsync(int orderId);
        Task<OrderEtaDto?> GetEtaAsync(int orderId);
    }
}


