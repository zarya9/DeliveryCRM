using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ContextDB _context;

        public NotificationService(ContextDB context)
        {
            _context = context;
        }

        public async Task SendAsync(int userId, int typeId, string title, string message, int? orderId = null)
        {
            var notification = new Notification
            {
                User_id = userId,
                Type_id = typeId,
                Title = title,
                Message = message,
                Order_id = orderId,
                Is_read = false,
                Sent_at = System.DateOnly.FromDateTime(System.DateTime.UtcNow)
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.User_id == userId)
                .Include(n => n.NotificationType)
                .Include(n => n.Order)
                .OrderByDescending(n => n.Sent_at)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.ID_Notification == notificationId);
            if (notification == null)
            {
                return;
            }

            notification.Is_read = true;
            await _context.SaveChangesAsync();
        }
    }
}


