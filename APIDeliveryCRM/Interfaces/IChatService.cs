using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IChatService
    {
        Task<IActionResult> SendMessageAsync(int chatRoomId, int senderId, string messageText, string? attachmentUrl = null);
        Task<IActionResult> GetMessagesAsync(int chatRoomId, int skip = 0, int take = 50);
        Task<IActionResult> GetChatRoomsForUserAsync(int userId);
        Task<IActionResult> CreateChatRoomAsync(int orderId, List<int> participantIds);
        Task<IActionResult> JoinChatRoomAsync(int chatRoomId, int userId);
        Task<IActionResult> LeaveChatRoomAsync(int chatRoomId, int userId);
        Task<IActionResult> MarkMessageAsReadAsync(int messageId, int userId);
        Task<IActionResult> EditMessageAsync(int messageId, int userId, string newText);
        Task<IActionResult> DeleteMessageAsync(int messageId, int userId);
        Task<IActionResult> GetUnreadMessagesCountAsync(int userId, int chatRoomId);
        Task<IActionResult> MarkAllMessagesAsReadAsync(int chatRoomId, int userId);
        Task<IActionResult> GetQuickReplyTemplatesAsync(int companyId, int userId, string? category = null, string? search = null);
        Task<IActionResult> UpsertQuickReplyTemplateAsync(int companyId, int userId, UpsertChatQuickReplyTemplateRequest request);
        Task<IActionResult> DeleteQuickReplyTemplateAsync(int companyId, int userId, int templateId);
    }
}

