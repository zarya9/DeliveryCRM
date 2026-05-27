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
        Task<IReadOnlyList<OrderStatusListItemDto>> GetOrderStatusesListAsync();
        Task<CustomerOrderCreateResult> CreateMineFromCustomerAsync(int userId, CustomerCreateOrderRequest request);
        Task<Order> CreateAsync(CreateOrderRequest request);
        Task<Order> UpdateAsync(Order order);
        Task<bool> ChangeStatusAsync(int orderId, int statusId, int? actorUserId = null);
        Task<(bool ok, RouteStopCompletionResultDto? result, string? error)> CompleteRouteStopAsync(int assignmentId, int courierProfileId, int? actorUserId = null);
        Task<IReadOnlyList<NearbyDeliveryStopDto>> GetNearbyDeliverableStopsAsync(int courierProfileId, double lat, double lon, double maxMeters = 15);
        Task<bool> AssignCourierAsync(int orderId, int courierProfileId);
        Task<OrderDispatchDto?> AutoDispatchAsync(int orderId);
        Task<bool> ManualOverrideCourierAsync(int orderId, int courierProfileId, string? reason, int? actorUserId = null);
        Task<(bool ok, string? error)> RevokeCourierAsync(int orderId, int? actorUserId = null, string? reason = null);
        Task<RevokeCourierOrdersResultDto> RevokeCourierOrdersAsync(
            int companyId,
            int courierProfileId,
            IReadOnlyList<int>? orderIds,
            int? actorUserId = null,
            string? reason = null);
        Task<IReadOnlyList<OrderTimelineEvent>> GetTimelineAsync(int orderId);
        Task<OrderEtaDto?> GetEtaAsync(int orderId);
        Task<(bool ok, string? error)> ClientCompleteOrderPaymentAsync(int orderId, int userId);
        Task<(bool ok, string? error)> DeleteMineAsync(int orderId, int userId);
    }
}


