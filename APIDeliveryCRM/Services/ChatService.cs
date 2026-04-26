using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APIDeliveryCRM.Services
{
    public class ChatService : IChatService
    {
        private readonly ContextDB _context;
        private readonly IHubContext<Hubs.ChatHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ContextDB context, IHubContext<Hubs.ChatHub> hubContext, 
            INotificationService notificationService, ILogger<ChatService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> SendMessageAsync(int chatRoomId, int senderId, string messageText, string? attachmentUrl = null)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
            {
                return new NotFoundObjectResult(new { message = "Чат-комната не найдена" });
            }

            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null)
            {
                return new NotFoundObjectResult(new { message = "Отправитель не найден" });
            }

            var message = new ChatMessage
            {
                ChatRoom_id = chatRoomId,
                Sender_id = senderId,
                MessageText = messageText,
                AttachmentUrl = attachmentUrl,
                Sent_at = DateTime.UtcNow,
                Is_deleted = false
            };

            await _context.ChatMessages.AddAsync(message);
            
            chatRoom.LastMessage_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Отправляем сообщение через SignalR
            await _hubContext.Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
            {
                id = message.ID_ChatMessage,
                chatRoomId = chatRoomId,
                senderId = senderId,
                senderName = $"{sender.FName} {sender.Name}",
                messageText = messageText,
                attachmentUrl = attachmentUrl,
                sentAt = message.Sent_at
            });

            // Отправляем уведомления участникам, которые не в сети
            await SendNotificationsToParticipantsAsync(chatRoomId, senderId, sender, messageText, chatRoom);

            return new OkObjectResult(new { message = "Сообщение отправлено", messageId = message.ID_ChatMessage });
        }

        public async Task<IActionResult> GetMessagesAsync(int chatRoomId, int skip = 0, int take = 50)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ChatRoom_id == chatRoomId && !m.Is_deleted)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Sent_at)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new OkObjectResult(messages.OrderBy(m => m.Sent_at));
        }

        public async Task<IActionResult> GetChatRoomsForUserAsync(int userId)
        {
            var chatRooms = await _context.ChatParticipants
                .Where(cp => cp.User_id == userId && cp.Is_active)
                .Include(cp => cp.ChatRoom)
                    .ThenInclude(cr => cr.Order)
                .Include(cp => cp.ChatRoom)
                    .ThenInclude(cr => cr.ChatRoomType)
                .Select(cp => cp.ChatRoom)
                .ToListAsync();

            return new OkObjectResult(chatRooms);
        }

        public async Task<IActionResult> CreateChatRoomAsync(int orderId, List<int> participantIds)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return new NotFoundObjectResult(new { message = "Заказ не найден" });
            }

            // Получаем тип чата для заказа (предполагаем, что есть тип "Order")
            var orderChatType = await _context.ChatRoomTypes.FirstOrDefaultAsync(ct => ct.Name == "Заказ");
            if (orderChatType == null)
            {
                // Создаем тип чата, если его нет
                orderChatType = new ChatRoomType
                {
                    Name = "Заказ",
                    Description = "Чат для обсуждения заказа"
                };
                await _context.ChatRoomTypes.AddAsync(orderChatType);
                await _context.SaveChangesAsync();
            }

            var chatRoom = new ChatRoom
            {
                Name = $"Чат по заказу #{order.Order_Number}",
                ChatRoomType_id = orderChatType.ID_ChatRoomType,
                Order_id = orderId,
                Created_at = DateTime.UtcNow
            };

            await _context.ChatRooms.AddAsync(chatRoom);
            await _context.SaveChangesAsync();

            // Добавляем участников
            foreach (var participantId in participantIds)
            {
                var participant = new ChatParticipant
                {
                    ChatRoom_id = chatRoom.ID_ChatRoom,
                    User_id = participantId,
                    Joined_at = DateTime.UtcNow,
                    Is_active = true
                };
                await _context.ChatParticipants.AddAsync(participant);
            }

            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Чат-комната создана", chatRoomId = chatRoom.ID_ChatRoom });
        }

        public async Task<IActionResult> JoinChatRoomAsync(int chatRoomId, int userId)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
            {
                return new NotFoundObjectResult(new { message = "Чат-комната не найдена" });
            }

            var existingParticipant = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatRoom_id == chatRoomId && cp.User_id == userId);

            if (existingParticipant != null)
            {
                existingParticipant.Is_active = true;
                existingParticipant.Left_at = null;
                existingParticipant.Joined_at = DateTime.UtcNow;
            }
            else
            {
                var participant = new ChatParticipant
                {
                    ChatRoom_id = chatRoomId,
                    User_id = userId,
                    Joined_at = DateTime.UtcNow,
                    Is_active = true
                };
                await _context.ChatParticipants.AddAsync(participant);
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Пользователь присоединился к чату" });
        }

        public async Task<IActionResult> LeaveChatRoomAsync(int chatRoomId, int userId)
        {
            var participant = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatRoom_id == chatRoomId && cp.User_id == userId);

            if (participant == null)
            {
                return new NotFoundObjectResult(new { message = "Участник не найден" });
            }

            participant.Is_active = false;
            participant.Left_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Пользователь покинул чат" });
        }

        public async Task<IActionResult> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null)
            {
                return new NotFoundObjectResult(new { message = "Сообщение не найдено" });
            }

            var participant = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatRoom_id == message.ChatRoom_id && cp.User_id == userId);

            if (participant != null)
            {
                participant.LastRead_at = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new OkObjectResult(new { message = "Сообщение отмечено как прочитанное" });
        }

        public async Task<IActionResult> EditMessageAsync(int messageId, int userId, string newText)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null)
            {
                return new NotFoundObjectResult(new { message = "Сообщение не найдено" });
            }

            if (message.Sender_id != userId)
            {
                return new UnauthorizedObjectResult(new { message = "Вы можете редактировать только свои сообщения" });
            }

            message.MessageText = newText;
            message.Edited_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Отправляем обновление через SignalR
            await _hubContext.Clients.Group($"ChatRoom_{message.ChatRoom_id}").SendAsync("MessageEdited", new
            {
                messageId = messageId,
                newText = newText,
                editedAt = message.Edited_at
            });

            return new OkObjectResult(new { message = "Сообщение отредактировано" });
        }

        public async Task<IActionResult> DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null)
            {
                return new NotFoundObjectResult(new { message = "Сообщение не найдено" });
            }

            if (message.Sender_id != userId)
            {
                return new UnauthorizedObjectResult(new { message = "Вы можете удалять только свои сообщения" });
            }

            message.Is_deleted = true;
            await _context.SaveChangesAsync();

            // Отправляем уведомление через SignalR
            await _hubContext.Clients.Group($"ChatRoom_{message.ChatRoom_id}").SendAsync("MessageDeleted", new
            {
                messageId = messageId
            });

            return new OkObjectResult(new { message = "Сообщение удалено" });
        }

        public async Task<IActionResult> GetUnreadMessagesCountAsync(int userId, int chatRoomId)
        {
            var participant = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatRoom_id == chatRoomId && cp.User_id == userId);

            if (participant == null)
            {
                return new NotFoundObjectResult(new { message = "Участник не найден" });
            }

            var lastReadTime = participant.LastRead_at ?? participant.Joined_at;
            var unreadCount = await _context.ChatMessages
                .CountAsync(m => m.ChatRoom_id == chatRoomId 
                    && m.Sender_id != userId 
                    && !m.Is_deleted 
                    && m.Sent_at > lastReadTime);

            return new OkObjectResult(new { unreadCount = unreadCount });
        }

        public async Task<IActionResult> MarkAllMessagesAsReadAsync(int chatRoomId, int userId)
        {
            var participant = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatRoom_id == chatRoomId && cp.User_id == userId);

            if (participant == null)
            {
                return new NotFoundObjectResult(new { message = "Участник не найден" });
            }

            // Обновляем время последнего прочтения на текущее время
            participant.LastRead_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Отправляем уведомление через SignalR
            await _hubContext.Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("AllMessagesRead", new
            {
                chatRoomId = chatRoomId,
                userId = userId,
                readAt = participant.LastRead_at
            });

            return new OkObjectResult(new { message = "Все сообщения отмечены как прочитанные" });
        }

        public async Task<IActionResult> GetQuickReplyTemplatesAsync(int companyId, int userId, string? category = null, string? search = null)
        {
            var query = _context.ChatQuickReplyTemplates
                .AsNoTracking()
                .Where(t => t.Company_id == companyId && t.User_id == userId && t.Is_active);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(t => t.Category.ToLower() == category.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Title.Contains(search) || t.Content.Contains(search));

            var list = await query
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Title)
                .Select(t => new ChatQuickReplyTemplateDto
                {
                    Id = t.ID_ChatQuickReplyTemplate,
                    UserId = t.User_id,
                    Category = t.Category,
                    Title = t.Title,
                    Content = t.Content,
                    IsActive = t.Is_active,
                    CreatedAt = t.Created_at
                })
                .ToListAsync();

            return new OkObjectResult(list);
        }

        public async Task<IActionResult> UpsertQuickReplyTemplateAsync(int companyId, int userId, UpsertChatQuickReplyTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                return new BadRequestObjectResult(new { message = "Title and content are required." });

            ChatQuickReplyTemplate entity;
            if (request.TemplateId.HasValue)
            {
                var existing = await _context.ChatQuickReplyTemplates
                    .FirstOrDefaultAsync(t => t.ID_ChatQuickReplyTemplate == request.TemplateId.Value
                                           && t.Company_id == companyId
                                           && t.User_id == userId);
                if (existing is null)
                    return new NotFoundObjectResult(new { message = "Template not found." });
                entity = existing;
            }
            else
            {
                entity = new ChatQuickReplyTemplate
                {
                    Company_id = companyId,
                    User_id = userId,
                    Created_at = DateTime.UtcNow
                };
                await _context.ChatQuickReplyTemplates.AddAsync(entity);
            }

            entity.Category = request.Category.Trim().ToLowerInvariant();
            entity.Title = request.Title.Trim();
            entity.Content = request.Content.Trim();
            entity.Is_active = request.IsActive;

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { id = entity.ID_ChatQuickReplyTemplate });
        }

        public async Task<IActionResult> DeleteQuickReplyTemplateAsync(int companyId, int userId, int templateId)
        {
            var entity = await _context.ChatQuickReplyTemplates
                .FirstOrDefaultAsync(t => t.ID_ChatQuickReplyTemplate == templateId
                                       && t.Company_id == companyId
                                       && t.User_id == userId);
            if (entity is null)
                return new NotFoundObjectResult(new { message = "Template not found." });

            _context.ChatQuickReplyTemplates.Remove(entity);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Template deleted." });
        }

        /// <summary>
        /// Отправляет уведомления участникам чата о новом сообщении
        /// </summary>
        private async Task SendNotificationsToParticipantsAsync(int chatRoomId, int senderId, User sender, 
            string messageText, ChatRoom chatRoom)
        {
            try
            {
                // Получаем всех активных участников чата, кроме отправителя
                var participants = await _context.ChatParticipants
                    .Where(cp => cp.ChatRoom_id == chatRoomId 
                        && cp.User_id != senderId 
                        && cp.Is_active)
                    .Include(cp => cp.User)
                    .ToListAsync();

                if (!participants.Any())
                    return;

                // Получаем или создаем тип уведомления для чата
                var chatNotificationType = await _context.NotificationTypes
                    .FirstOrDefaultAsync(nt => nt.Name == "Новое сообщение в чате" || nt.Name == "ChatMessage" || nt.Name.Contains("чат"));

                if (chatNotificationType == null)
                {
                    // Создаем тип уведомления, если его нет
                    chatNotificationType = new NotificationType
                    {
                        Name = "Новое сообщение в чате",
                        Description = "Уведомление о новом сообщении в чате"
                    };
                    await _context.NotificationTypes.AddAsync(chatNotificationType);
                    await _context.SaveChangesAsync();
                }

                // Обрезаем текст сообщения для уведомления (максимум 100 символов)
                var notificationText = messageText.Length > 100 
                    ? messageText.Substring(0, 100) + "..." 
                    : messageText;

                var senderName = $"{sender.FName} {sender.Name}";
                var chatRoomName = chatRoom.Name ?? $"Чат #{chatRoomId}";

                // Отправляем уведомления всем участникам
                foreach (var participant in participants)
                {
                    try
                    {
                        await _notificationService.SendAsync(
                            userId: participant.User_id,
                            typeId: chatNotificationType.ID_NotificationType,
                            title: $"Новое сообщение в {chatRoomName}",
                            message: $"{senderName}: {notificationText}",
                            orderId: chatRoom.Order_id
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при отправке уведомления пользователю {UserId}", participant.User_id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомлений о новом сообщении в чате {ChatRoomId}", chatRoomId);
            }
        }
    }
}

