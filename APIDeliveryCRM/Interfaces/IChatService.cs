using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IChatService
    {
        Task<IActionResult> SendMessageAsync(int chatRoomId, int senderId, string? messageText, string? attachmentUrl = null, int? replyToMessageId = null, List<int>? mentionedUserIds = null);
        Task<IActionResult> GetMessagesAsync(int chatRoomId, int userId, int skip = 0, int take = 50);
        Task<IActionResult> GetChatRoomsForUserAsync(int userId);
        Task<IActionResult> CreateChatRoomAsync(int orderId, List<int> participantIds);
        Task<IActionResult> JoinChatRoomAsync(int chatRoomId, int userId);
        Task<IActionResult> LeaveChatRoomAsync(int chatRoomId, int userId);
        Task<IActionResult> MarkMessageAsReadAsync(int messageId, int userId);
        Task<IActionResult> EditMessageAsync(int messageId, int userId, string newText);
        Task<IActionResult> DeleteMessageAsync(int messageId, int userId);
        Task<IActionResult> ModerateDeleteMessageAsync(int messageId, int moderatorUserId);
        Task<IActionResult> GetUnreadMessagesCountAsync(int userId, int chatRoomId);
        Task<IActionResult> MarkAllMessagesAsReadAsync(int chatRoomId, int userId);
        Task<IActionResult> GetChatRoomsListAsync(int companyId, int userId);
        Task<int?> GetUserCompanyIdAsync(int userId);
        Task<IActionResult> GetOrCreateCompanyRoomAsync(int companyId, int userId);
        Task<IActionResult> CreateOrGetDirectRoomAsync(int companyId, int userId, int peerUserId);
        Task<IActionResult> GetOrCreateOrderRoomAsync(int orderId, int userId, int? peerUserId = null);
        Task<IActionResult> GetQuickReplyTemplatesAsync(int companyId, int userId, string? category = null, string? search = null);
        Task<IActionResult> UpsertQuickReplyTemplateAsync(int companyId, int userId, UpsertChatQuickReplyTemplateRequest request);
        Task<IActionResult> DeleteQuickReplyTemplateAsync(int companyId, int userId, int templateId);

        // Управление участниками
        Task<IActionResult> GetParticipantsAsync(int chatRoomId, int requestingUserId);
        Task<IActionResult> AddParticipantAsync(int chatRoomId, int targetUserId, int requestingUserId);
        Task<IActionResult> RemoveParticipantAsync(int chatRoomId, int targetUserId, int requestingUserId);

        // Поиск по сообщениям
        Task<IActionResult> SearchMessagesAsync(int chatRoomId, int userId, string searchText, int skip = 0, int take = 50);

        // Реакции
        Task<IActionResult> AddReactionAsync(int messageId, int userId, string emoji);
        Task<IActionResult> RemoveReactionAsync(int messageId, int userId, string emoji);
        Task<IActionResult> GetReactionsAsync(int messageId, int userId);
    }
}

