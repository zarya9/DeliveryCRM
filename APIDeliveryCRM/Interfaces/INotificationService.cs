using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(int userId, int typeId, string title, string message, int? orderId = null, byte priority = 0, bool isCritical = false, bool requiresAck = false);
        Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool onlyCritical = false, bool onlyUnread = false, byte? minPriority = null, bool onlyRequiresAck = false);
        /// <summary>Пометить прочитанным только если уведомление принадлежит пользователю.</summary>
        Task<bool> MarkAsReadForUserAsync(int notificationId, int userId);
        Task<bool> AcknowledgeForUserAsync(int notificationId, int userId);
    }
}


