using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("rooms")]
        public async Task<IActionResult> CreateChatRoom([FromQuery] int orderId, [FromBody] List<int> participantIds)
        {
            return await _chatService.CreateChatRoomAsync(orderId, participantIds);
        }

        [HttpGet("rooms/user/{userId:int}")]
        public async Task<IActionResult> GetChatRooms(int userId)
        {
            return await _chatService.GetChatRoomsForUserAsync(userId);
        }

        [HttpPost("rooms/{chatRoomId:int}/join")]
        public async Task<IActionResult> JoinChatRoom(int chatRoomId, [FromQuery] int userId)
        {
            return await _chatService.JoinChatRoomAsync(chatRoomId, userId);
        }

        [HttpPost("rooms/{chatRoomId:int}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int chatRoomId, [FromQuery] int userId)
        {
            return await _chatService.LeaveChatRoomAsync(chatRoomId, userId);
        }

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromQuery] int chatRoomId, [FromQuery] int senderId, [FromBody] SendMessageRequest request)
        {
            return await _chatService.SendMessageAsync(chatRoomId, senderId, request.MessageText, request.AttachmentUrl);
        }

        [HttpGet("rooms/{chatRoomId:int}/messages")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            return await _chatService.GetMessagesAsync(chatRoomId, skip, take);
        }

        [HttpPost("messages/{messageId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int messageId, [FromQuery] int userId)
        {
            return await _chatService.MarkMessageAsReadAsync(messageId, userId);
        }

        [HttpPut("messages/{messageId:int}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromQuery] int userId, [FromBody] string newText)
        {
            return await _chatService.EditMessageAsync(messageId, userId, newText);
        }

        [HttpDelete("messages/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(int messageId, [FromQuery] int userId)
        {
            return await _chatService.DeleteMessageAsync(messageId, userId);
        }

        [HttpGet("rooms/{chatRoomId:int}/unread")]
        public async Task<IActionResult> GetUnreadCount(int chatRoomId, [FromQuery] int userId)
        {
            return await _chatService.GetUnreadMessagesCountAsync(userId, chatRoomId);
        }

        [HttpPost("rooms/{chatRoomId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int chatRoomId, [FromQuery] int userId)
        {
            return await _chatService.MarkAllMessagesAsReadAsync(chatRoomId, userId);
        }
    }
}

