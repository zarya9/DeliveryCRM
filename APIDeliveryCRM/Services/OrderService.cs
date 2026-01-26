using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class OrderService : IOrderService
    {
        private readonly ContextDB _context;

        public OrderService(ContextDB context)
        {
            _context = context;
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.ClientProfile)
                .Include(o => o.CourierProfile)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderType)
                .Include(o => o.PackageType)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.ID_Order == id);
        }

        public async Task<IReadOnlyList<Order>> GetByClientAsync(int clientProfileId)
        {
            return await _context.Orders
                .Where(o => o.Client_id == clientProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.CourierProfile)
                    .ThenInclude(c => c.User)
                .Include(o => o.OrderType)
                .Include(o => o.PackageType)
                .Include(o => o.PaymentMethod)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .OrderByDescending(o => o.Created_at)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetByCourierAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .ToListAsync();
        }

        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            // Генерируем номер заказа
            var maxOrderNumber = await _context.Orders
                .Select(o => o.Order_Number)
                .DefaultIfEmpty(0)
                .MaxAsync();
            
            var order = new Order
            {
                Name = request.Name,
                Description = request.Description,
                Order_Number = maxOrderNumber + 1,
                Client_id = request.Client_id,
                OrderType_id = request.OrderType_id,
                Status_id = request.Status_id,
                Courier_id = request.Courier_id,
                PackageType_id = request.PackageType_id,
                Weight = request.Weight,
                Height = request.Height,
                Length = request.Length,
                Width = request.Width,
                Estimated_cost = request.Estimated_cost,
                Final_cost = 0,
                Created_at = DateTime.UtcNow,
                PaymentMethod_id = request.PaymentMethod_id,
                Is_paid = false,
                PickupAddress_id = request.PickupAddress_id,
                DeliveryAddress_id = request.DeliveryAddress_id
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            order.Status_id = statusId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignCourierAsync(int orderId, int courierProfileId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            order.Courier_id = courierProfileId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


