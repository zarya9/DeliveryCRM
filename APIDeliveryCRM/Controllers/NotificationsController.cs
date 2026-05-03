using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>Список уведомлений текущего пользователя (JWT).</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMine([FromQuery] bool onlyCritical = false, [FromQuery] bool onlyUnread = false, [FromQuery] byte? minPriority = null, [FromQuery] bool onlyRequiresAck = false)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var notifications = await _notificationService.GetForUserAsync(userId.Value, onlyCritical, onlyUnread, minPriority, onlyRequiresAck);
            return Ok(notifications.Select(MapToDto).ToList());
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetForUser(int userId)
        {
            var currentId = GetCurrentUserId();
            if (!currentId.HasValue || currentId.Value != userId)
                return Forbid();

            var notifications = await _notificationService.GetForUserAsync(userId);
            return Ok(notifications.Select(MapToDto).ToList());
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var ok = await _notificationService.MarkAsReadForUserAsync(id, userId.Value);
            if (!ok)
                return NotFound();
            return Ok();
        }

        [HttpPost("{id:int}/ack")]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var ok = await _notificationService.AcknowledgeForUserAsync(id, userId.Value);
            if (!ok)
                return BadRequest(new { message = "Уведомление не найдено или не требует подтверждения." });
            return Ok();
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private static NotificationItemDto MapToDto(Notification n)
        {
            return new NotificationItemDto
            {
                Id = n.ID_Notification,
                Title = n.Title ?? "",
                Message = n.Message ?? "",
                TypeName = n.NotificationType?.Name,
                OrderId = n.Order_id,
                IsRead = n.Is_read,
                SentAt = n.Sent_at,
                Priority = n.Priority,
                IsCritical = n.Is_critical,
                RequiresAck = n.Requires_ack,
                AcknowledgedAt = n.Acknowledged_at
            };
        }
    }
}
