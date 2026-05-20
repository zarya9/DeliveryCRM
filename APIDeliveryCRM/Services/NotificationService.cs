using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Hubs;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ContextDB _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(ContextDB context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendAsync(int userId, int typeId, string title, string message, int? orderId = null, int? shiftId = null, byte priority = 0, bool isCritical = false, bool requiresAck = false)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ID_User == userId);
            if (user == null)
                return;

            var notification = new Notification
            {
                Company_id = user.Company_id,
                User_id = userId,
                Type_id = typeId,
                Title = title,
                Message = message,
                Order_id = orderId,
                Shift_id = shiftId,
                Is_read = false,
                Priority = priority,
                Is_critical = isCritical,
                Requires_ack = requiresAck,
                Sent_at = System.DateOnly.FromDateTime(System.DateTime.UtcNow)
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"User_{userId}").SendAsync("NotificationReceived", new
            {
                id = notification.ID_Notification,
                title = notification.Title,
                message = notification.Message,
                typeId = notification.Type_id,
                orderId = notification.Order_id,
                shiftId = notification.Shift_id,
                priority = notification.Priority,
                isCritical = notification.Is_critical,
                requiresAck = notification.Requires_ack,
                sentAt = notification.Sent_at.ToString("yyyy-MM-dd")
            });
        }

        public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool onlyCritical = false, bool onlyUnread = false, byte? minPriority = null, bool onlyRequiresAck = false)
        {
            var query = _context.Notifications
                .Where(n => n.User_id == userId)
                .Include(n => n.NotificationType)
                .Include(n => n.Order)
                .AsQueryable();

            if (onlyCritical)
                query = query.Where(n => n.Is_critical);
            if (onlyUnread)
                query = query.Where(n => !n.Is_read);
            if (minPriority.HasValue)
                query = query.Where(n => n.Priority >= minPriority.Value);
            if (onlyRequiresAck)
                query = query.Where(n => n.Requires_ack && n.Acknowledged_at == null);

            return await query
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.Sent_at)
                .Take(500)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadForUserAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.ID_Notification == notificationId && n.User_id == userId);
            if (notification == null)
                return false;

            notification.Is_read = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AcknowledgeForUserAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.ID_Notification == notificationId && n.User_id == userId);
            if (notification == null)
                return false;

            if (!notification.Requires_ack)
                return false;

            notification.Acknowledged_at = System.DateTime.UtcNow;
            notification.Is_read = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


