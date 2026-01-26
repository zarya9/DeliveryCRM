using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace APIDeliveryCRM.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinRoom(int chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task LeaveRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserLeft", Context.ConnectionId);
        }

        public async Task SendMessage(int chatRoomId, int senderId, string messageText, string? attachmentUrl = null)
        {
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
            {
                chatRoomId = chatRoomId,
                senderId = senderId,
                messageText = messageText,
                attachmentUrl = attachmentUrl,
                sentAt = DateTime.UtcNow
            });
        }

        public async Task Typing(int chatRoomId, int userId, bool isTyping)
        {
            await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserTyping", new
                {
                    chatRoomId = chatRoomId,
                    userId = userId,
                    isTyping = isTyping
                });
        }

        public async Task MarkAsRead(int chatRoomId, int userId, int messageId)
        {
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("MessageRead", new
            {
                chatRoomId = chatRoomId,
                userId = userId,
                messageId = messageId,
                readAt = DateTime.UtcNow
            });
        }
    }
}

