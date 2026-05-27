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

        private async Task<(int? userId, int? companyId)> ResolveChatContextAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return (null, null);

            var companyId = GetCurrentCompanyId();
            if (companyId is > 0)
                return (userId, companyId);

            companyId = await _chatService.GetUserCompanyIdAsync(userId.Value);
            return companyId is > 0 ? (userId, companyId) : (userId, null);
        }

        // ─── Комнаты ──────────────────────────────────────────────────────────

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

        [HttpGet("rooms/list")]
        public async Task<IActionResult> GetMyRoomsList()
        {
            var (userId, companyId) = await ResolveChatContextAsync();
            if (!userId.HasValue || !companyId.HasValue)
                return Unauthorized();
            return await _chatService.GetChatRoomsListAsync(companyId.Value, userId.Value);
        }

        [HttpPost("rooms/company")]
        [Authorize(Roles = "Администратор,Админ,Менеджер,Логист,Курьер,Система")]
        public async Task<IActionResult> EnsureCompanyRoom()
        {
            var (userId, companyId) = await ResolveChatContextAsync();
            if (!userId.HasValue || !companyId.HasValue)
                return Unauthorized();
            return await _chatService.GetOrCreateCompanyRoomAsync(companyId.Value, userId.Value);
        }

        [HttpPost("rooms/direct")]
        public async Task<IActionResult> CreateOrGetDirectRoom([FromQuery] int peerUserId)
        {
            var (userId, companyId) = await ResolveChatContextAsync();
            if (!userId.HasValue || !companyId.HasValue)
                return Unauthorized();
            return await _chatService.CreateOrGetDirectRoomAsync(companyId.Value, userId.Value, peerUserId);
        }

        [HttpPost("rooms/order")]
        public async Task<IActionResult> CreateOrGetOrderRoom([FromQuery] int orderId, [FromQuery] int? peerUserId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.GetOrCreateOrderRoomAsync(orderId, currentUserId.Value, peerUserId);
        }

        [HttpGet("rooms/user/{userId:int}")]
        [Authorize(Roles = "Администратор,Админ")]
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

        // ─── Участники ────────────────────────────────────────────────────────

        [HttpGet("rooms/{chatRoomId:int}/participants")]
        public async Task<IActionResult> GetParticipants(int chatRoomId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.GetParticipantsAsync(chatRoomId, currentUserId.Value);
        }

        [HttpPost("rooms/{chatRoomId:int}/participants")]
        [Authorize(Roles = "Администратор,Админ,Менеджер")]
        public async Task<IActionResult> AddParticipant(int chatRoomId, [FromQuery] int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.AddParticipantAsync(chatRoomId, userId, currentUserId.Value);
        }

        [HttpDelete("rooms/{chatRoomId:int}/participants/{userId:int}")]
        public async Task<IActionResult> RemoveParticipant(int chatRoomId, int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.RemoveParticipantAsync(chatRoomId, userId, currentUserId.Value);
        }

        // ─── Сообщения ────────────────────────────────────────────────────────

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromQuery] int chatRoomId, [FromBody] SendMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.SendMessageAsync(
                chatRoomId,
                currentUserId.Value,
                request.MessageText,
                request.AttachmentUrl,
                request.ReplyToMessageId,
                request.MentionedUserIds);
        }

        [HttpGet("rooms/{chatRoomId:int}/messages")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.GetMessagesAsync(chatRoomId, currentUserId.Value, skip, take);
        }

        [HttpGet("rooms/{chatRoomId:int}/messages/search")]
        public async Task<IActionResult> SearchMessages(int chatRoomId, [FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.SearchMessagesAsync(chatRoomId, currentUserId.Value, q, skip, take);
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

        /// <summary>Модерация — менеджер/администратор удаляет чужое сообщение.</summary>
        [HttpDelete("messages/{messageId:int}/moderate")]
        [Authorize(Roles = "Администратор,Админ,Менеджер")]
        public async Task<IActionResult> ModerateDeleteMessage(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.ModerateDeleteMessageAsync(messageId, currentUserId.Value);
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

        // ─── Реакции ──────────────────────────────────────────────────────────

        [HttpPost("messages/{messageId:int}/reactions")]
        public async Task<IActionResult> AddReaction(int messageId, [FromQuery] string emoji)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.AddReactionAsync(messageId, currentUserId.Value, emoji);
        }

        [HttpDelete("messages/{messageId:int}/reactions")]
        public async Task<IActionResult> RemoveReaction(int messageId, [FromQuery] string emoji)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.RemoveReactionAsync(messageId, currentUserId.Value, emoji);
        }

        [HttpGet("messages/{messageId:int}/reactions")]
        public async Task<IActionResult> GetReactions(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();
            return await _chatService.GetReactionsAsync(messageId, currentUserId.Value);
        }

        // ─── Быстрые ответы ───────────────────────────────────────────────────

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

