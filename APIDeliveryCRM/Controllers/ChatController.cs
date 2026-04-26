using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private int? GetCurrentCompanyId()
        {
            var v = User.FindFirst("companyId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        [HttpPost("rooms")]
        public async Task<IActionResult> CreateChatRoom([FromQuery] int orderId, [FromBody] List<int> participantIds)
        {
            return await _chatService.CreateChatRoomAsync(orderId, participantIds);
        }

        [HttpGet("rooms/user/me")]
        public async Task<IActionResult> GetMyChatRooms()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.GetChatRoomsForUserAsync(currentUserId.Value);
        }

        [HttpGet("rooms/user/{userId:int}")]
        [Authorize(Roles = "Админ")]
        public async Task<IActionResult> GetChatRooms(int userId)
        {
            return await _chatService.GetChatRoomsForUserAsync(userId);
        }

        [HttpPost("rooms/{chatRoomId:int}/join")]
        public async Task<IActionResult> JoinChatRoom(int chatRoomId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.JoinChatRoomAsync(chatRoomId, currentUserId.Value);
        }

        [HttpPost("rooms/{chatRoomId:int}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int chatRoomId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.LeaveChatRoomAsync(chatRoomId, currentUserId.Value);
        }

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromQuery] int chatRoomId, [FromBody] SendMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.SendMessageAsync(chatRoomId, currentUserId.Value, request.MessageText, request.AttachmentUrl);
        }

        [HttpGet("rooms/{chatRoomId:int}/messages")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            return await _chatService.GetMessagesAsync(chatRoomId, skip, take);
        }

        [HttpPost("messages/{messageId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.MarkMessageAsReadAsync(messageId, currentUserId.Value);
        }

        [HttpPut("messages/{messageId:int}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] string newText)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.EditMessageAsync(messageId, currentUserId.Value, newText);
        }

        [HttpDelete("messages/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.DeleteMessageAsync(messageId, currentUserId.Value);
        }

        [HttpGet("rooms/{chatRoomId:int}/unread")]
        public async Task<IActionResult> GetUnreadCount(int chatRoomId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.GetUnreadMessagesCountAsync(currentUserId.Value, chatRoomId);
        }

        [HttpPost("rooms/{chatRoomId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int chatRoomId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            return await _chatService.MarkAllMessagesAsReadAsync(chatRoomId, currentUserId.Value);
        }

        [HttpGet("quick-replies")]
        public async Task<IActionResult> GetQuickReplies([FromQuery] string? category = null, [FromQuery] string? search = null)
        {
            var currentUserId = GetCurrentUserId();
            var currentCompanyId = GetCurrentCompanyId();
            if (!currentUserId.HasValue || !currentCompanyId.HasValue)
                return Unauthorized();

            return await _chatService.GetQuickReplyTemplatesAsync(currentCompanyId.Value, currentUserId.Value, category, search);
        }

        [HttpPost("quick-replies")]
        public async Task<IActionResult> UpsertQuickReply([FromBody] UpsertChatQuickReplyTemplateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var currentCompanyId = GetCurrentCompanyId();
            if (!currentUserId.HasValue || !currentCompanyId.HasValue)
                return Unauthorized();

            return await _chatService.UpsertQuickReplyTemplateAsync(currentCompanyId.Value, currentUserId.Value, request);
        }

        [HttpDelete("quick-replies/{templateId:int}")]
        public async Task<IActionResult> DeleteQuickReply(int templateId)
        {
            var currentUserId = GetCurrentUserId();
            var currentCompanyId = GetCurrentCompanyId();
            if (!currentUserId.HasValue || !currentCompanyId.HasValue)
                return Unauthorized();

            return await _chatService.DeleteQuickReplyTemplateAsync(currentCompanyId.Value, currentUserId.Value, templateId);
        }
    }
}

