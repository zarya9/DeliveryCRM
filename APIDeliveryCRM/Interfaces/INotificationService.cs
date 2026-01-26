using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(int userId, int typeId, string title, string message, int? orderId = null);
        Task<IReadOnlyList<Notification>> GetForUserAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
    }
}


