using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Interfaces
{
    public interface IOrderService
    {
        Task<Order> GetByIdAsync(int id);
        Task<IReadOnlyList<Order>> GetByClientAsync(int clientProfileId);
        Task<IReadOnlyList<Order>> GetByCourierAsync(int courierProfileId);
        Task<Order> CreateAsync(CreateOrderRequest request);
        Task<Order> UpdateAsync(Order order);
        Task<bool> ChangeStatusAsync(int orderId, int statusId);
        Task<bool> AssignCourierAsync(int orderId, int courierProfileId);
    }
}


