using System.Security.Claims;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace APIDeliveryCRM.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUserPresenceService _presence;

        public ChatHub(IUserPresenceService presence)
        {
            _presence = presence;
        }

        private static string CompanyGroup(int companyId) => $"Company_{companyId}";
        private static string UserGroup(int userId) => $"User_{userId}";

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            var companyId = GetCompanyIdFromContext();

            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
                _presence.UserConnected(userId.Value);
            }

            if (companyId.HasValue)
                await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(companyId.Value));

            if (companyId.HasValue && userId.HasValue)
            {
                await Clients.Group(CompanyGroup(companyId.Value))
                    .SendAsync("UserPresenceChanged", new { userId = userId.Value, online = true });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            var companyId = GetCompanyIdFromContext();

            if (userId.HasValue)
            {
                _presence.UserDisconnected(userId.Value);
            }

            if (companyId.HasValue && userId.HasValue)
            {
                var stillOnline = _presence.IsUserOnline(userId.Value);
                await Clients.Group(CompanyGroup(companyId.Value))
                    .SendAsync("UserPresenceChanged", new { userId = userId.Value, online = stillOnline });
            }

            await base.OnDisconnectedAsync(exception);
        }

        private int? GetUserIdFromContext()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value;
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private int? GetCompanyIdFromContext()
        {
            var v = Context.User?.FindFirst("companyId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        public async Task JoinRoom(int chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
                _presence.SetViewingRoom(userId.Value, chatRoomId);
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task LeaveRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
                _presence.ClearViewingRoom(userId.Value, chatRoomId);
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserLeft", Context.ConnectionId);
        }

        public async Task SendMessage(int chatRoomId, int senderId, string messageText, string? attachmentUrl = null)
        {
            // Используем id пользователя из токена, а не параметр senderId
            var currentUserId = GetUserIdFromContext();
            if (!currentUserId.HasValue)
                return;

            // Только трансляция — сохранение через  POST /api/chat/messages
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
            {
                chatRoomId,
                senderId = currentUserId.Value,
                messageText,
                attachmentUrl,
                sentAt = DateTime.UtcNow
            });
        }

        public async Task Typing(int chatRoomId, int userId, bool isTyping)
        {
            await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserTyping", new
                {
                    chatRoomId,
                    userId,
                    isTyping
                });
        }

        public async Task MarkAsRead(int chatRoomId, int userId, int messageId)
        {
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("MessageRead", new
            {
                chatRoomId,
                userId,
                messageId,
                readAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Клиент сообщает, что сообщение доставлено (DeliveryStatus = Delivered).
        /// Сервер транслирует отправителю.
        /// </summary>
        public async Task MessageDelivered(int chatRoomId, int messageId)
        {
            var userId = GetUserIdFromContext();
            if (!userId.HasValue) return;

            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("MessageDelivered", new
            {
                chatRoomId,
                messageId,
                deliveredToUserId = userId.Value,
                deliveredAt = DateTime.UtcNow
            });
        }
    }
}

