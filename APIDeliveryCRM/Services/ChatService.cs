using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Hubs;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class ChatService : IChatService
{
    private readonly ContextDB _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IChatMessageCryptoService _crypto;

    public ChatService(ContextDB context, IHubContext<ChatHub> hubContext, IChatMessageCryptoService crypto)
    {
        _context = context;
        _hubContext = hubContext;
        _crypto = crypto;
    }

    public async Task<IActionResult> SendMessageAsync(int chatRoomId, int senderId, string? messageText, string? attachmentUrl = null)
    {
        var room = await _context.ChatRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID_ChatRoom == chatRoomId);
        if (room == null)
            return new NotFoundObjectResult(new { message = "Чат не найден." });

        var isParticipant = await _context.ChatParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == senderId && p.Is_active);
        if (!isParticipant)
            return new ForbidResult();

        if (string.IsNullOrWhiteSpace(messageText) && string.IsNullOrWhiteSpace(attachmentUrl))
            return new BadRequestObjectResult(new { message = "Пустое сообщение." });

        var plainText = (messageText ?? string.Empty).Trim();
        var storedText = string.IsNullOrWhiteSpace(plainText) ? string.Empty : _crypto.Encrypt(plainText);

        var msg = new ChatMessage
        {
            ChatRoom_id = chatRoomId,
            Sender_id = senderId,
            MessageText = storedText,
            AttachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl) ? null : attachmentUrl.Trim(),
            Sent_at = DateTime.UtcNow,
            Is_deleted = false
        };
        _context.ChatMessages.Add(msg);

        var editableRoom = await _context.ChatRooms.FirstOrDefaultAsync(r => r.ID_ChatRoom == chatRoomId);
        if (editableRoom != null)
            editableRoom.LastMessage_at = msg.Sent_at;

        await _context.SaveChangesAsync();

        var senderName = await _context.Users.AsNoTracking()
            .Where(u => u.ID_User == senderId)
            .Select(u => ((u.FName ?? "") + " " + (u.Name ?? "")).Trim())
            .FirstOrDefaultAsync();

        var payload = new
        {
            id = msg.ID_ChatMessage,
            chatRoomId = msg.ChatRoom_id,
            senderId = msg.Sender_id,
            senderName = string.IsNullOrWhiteSpace(senderName) ? $"Пользователь #{senderId}" : senderName,
            messageText = plainText,
            attachmentUrl = msg.AttachmentUrl,
            sentAt = msg.Sent_at,
            editedAt = msg.Edited_at,
            isDeleted = msg.Is_deleted
        };
        await BroadcastToRoomParticipantsAsync(chatRoomId, "ReceiveMessage", payload);
        return new OkObjectResult(payload);
    }

    public async Task<IActionResult> GetMessagesAsync(int chatRoomId, int skip = 0, int take = 50)
    {
        if (take <= 0) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        var rows = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatRoom_id == chatRoomId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.Sent_at)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var list = rows
            .Select(m => new
            {
                ID_ChatMessage = m.ID_ChatMessage,
                ChatRoom_id = m.ChatRoom_id,
                Sender_id = m.Sender_id,
                MessageText = m.Is_deleted ? "[deleted]" : DecryptSafe(m.MessageText),
                SenderName = ((m.Sender?.FName ?? "") + " " + (m.Sender?.Name ?? "")).Trim(),
                AttachmentUrl = m.AttachmentUrl,
                Sent_at = m.Sent_at,
                Edited_at = m.Edited_at,
                Is_deleted = m.Is_deleted
            })
            .OrderBy(m => m.Sent_at)
            .ToList();

        return new OkObjectResult(list);
    }

    public async Task<IActionResult> GetChatRoomsForUserAsync(int userId)
    {
        var roomIds = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == userId && p.Is_active)
            .Select(p => p.ChatRoom_id)
            .Distinct()
            .ToListAsync();

        var rooms = await _context.ChatRooms.AsNoTracking()
            .Where(r => roomIds.Contains(r.ID_ChatRoom))
            .OrderByDescending(r => r.LastMessage_at ?? r.Created_at)
            .ToListAsync();
        return new OkObjectResult(rooms);
    }

    public async Task<IActionResult> CreateChatRoomAsync(int orderId, List<int> participantIds)
    {
        participantIds ??= new List<int>();
        participantIds = participantIds.Where(x => x > 0).Distinct().ToList();
        if (participantIds.Count == 0)
            return new BadRequestObjectResult(new { message = "Добавьте участников." });

        var order = await _context.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ID_Order == orderId);
        if (order == null)
            return new NotFoundObjectResult(new { message = "Заказ не найден." });

        var roomTypeId = await ResolveRoomTypeIdAsync("order", "Комната по заказу");
        var room = new ChatRoom
        {
            Company_id = order.Company_id,
            ChatRoomType_id = roomTypeId,
            Order_id = orderId,
            Name = $"Заказ #{order.Order_Number}",
            Created_at = DateTime.UtcNow
        };
        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();

        foreach (var uid in participantIds)
        {
            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatRoom_id = room.ID_ChatRoom,
                User_id = uid,
                Joined_at = DateTime.UtcNow,
                Is_active = true
            });
        }
        await _context.SaveChangesAsync();
        return new OkObjectResult(new { roomId = room.ID_ChatRoom, roomName = room.Name });
    }

    public async Task<IActionResult> JoinChatRoomAsync(int chatRoomId, int userId)
    {
        var roomExists = await _context.ChatRooms.AsNoTracking().AnyAsync(r => r.ID_ChatRoom == chatRoomId);
        if (!roomExists)
            return new NotFoundResult();

        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == userId);
        if (participant == null)
        {
            participant = new ChatParticipant
            {
                ChatRoom_id = chatRoomId,
                User_id = userId,
                Joined_at = DateTime.UtcNow,
                Is_active = true
            };
            _context.ChatParticipants.Add(participant);
        }
        else
        {
            participant.Is_active = true;
            participant.Left_at = null;
        }

        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> LeaveChatRoomAsync(int chatRoomId, int userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == userId && p.Is_active);
        if (participant == null)
            return new NotFoundResult();

        participant.Is_active = false;
        participant.Left_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> MarkMessageAsReadAsync(int messageId, int userId)
    {
        var msg = await _context.ChatMessages.AsNoTracking().FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();

        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == msg.ChatRoom_id && p.User_id == userId);
        if (participant == null)
            return new ForbidResult();

        participant.LastRead_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group($"ChatRoom_{msg.ChatRoom_id}")
            .SendAsync("MessageRead", new { chatRoomId = msg.ChatRoom_id, userId, messageId, readAt = participant.LastRead_at });
        return new OkResult();
    }

    public async Task<IActionResult> EditMessageAsync(int messageId, int userId, string newText)
    {
        var msg = await _context.ChatMessages.FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();
        if (msg.Sender_id != userId)
            return new ForbidResult();

        var clean = (newText ?? string.Empty).Trim();
        if (clean.Length == 0)
            return new BadRequestObjectResult(new { message = "Пустой текст." });

        msg.MessageText = _crypto.Encrypt(clean);
        msg.Edited_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(msg.ChatRoom_id, "MessageEdited",
            new { messageId, chatRoomId = msg.ChatRoom_id, messageText = clean, editedAt = msg.Edited_at });
        return new OkResult();
    }

    public async Task<IActionResult> DeleteMessageAsync(int messageId, int userId)
    {
        var msg = await _context.ChatMessages.FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();
        if (msg.Sender_id != userId)
            return new ForbidResult();

        msg.Is_deleted = true;
        msg.MessageText = string.Empty;
        msg.AttachmentUrl = null;
        msg.Edited_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(msg.ChatRoom_id, "MessageDeleted",
            new { messageId, chatRoomId = msg.ChatRoom_id });
        return new OkResult();
    }

    public async Task<IActionResult> GetUnreadMessagesCountAsync(int userId, int chatRoomId)
    {
        var participant = await _context.ChatParticipants.AsNoTracking()
            .FirstOrDefaultAsync(p => p.User_id == userId && p.ChatRoom_id == chatRoomId && p.Is_active);
        if (participant == null)
            return new OkObjectResult(new { unreadCount = 0 });

        var count = await _context.ChatMessages.AsNoTracking()
            .Where(m => m.ChatRoom_id == chatRoomId &&
                        !m.Is_deleted &&
                        m.Sender_id != userId &&
                        (!participant.LastRead_at.HasValue || m.Sent_at > participant.LastRead_at.Value))
            .CountAsync();

        return new OkObjectResult(new { unreadCount = count });
    }

    public async Task<IActionResult> MarkAllMessagesAsReadAsync(int chatRoomId, int userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == userId && p.Is_active);
        if (participant == null)
            return new NotFoundResult();

        participant.LastRead_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> GetChatRoomsListAsync(int companyId, int userId)
    {
        var myParticipants = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == userId && p.Is_active)
            .Select(p => new { p.ChatRoom_id, p.LastRead_at })
            .ToListAsync();

        var roomIds = myParticipants.Select(x => x.ChatRoom_id).ToArray();
        if (roomIds.Length == 0)
            return new OkObjectResult(new List<object>());

        var rooms = await _context.ChatRooms.AsNoTracking()
            .Include(r => r.ChatRoomType)
            .Include(r => r.Participants)
            .Where(r => r.Company_id == companyId && roomIds.Contains(r.ID_ChatRoom))
            .ToListAsync();

        var messages = await _context.ChatMessages.AsNoTracking()
            .Where(m => roomIds.Contains(m.ChatRoom_id))
            .OrderByDescending(m => m.Sent_at)
            .ToListAsync();

        var users = await _context.Users.AsNoTracking()
            .Where(u => u.Company_id == companyId)
            .Select(u => new { u.ID_User, u.Name, u.FName })
            .ToListAsync();

        var list = new List<object>();
        foreach (var room in rooms.OrderByDescending(r => r.LastMessage_at ?? r.Created_at))
        {
            var mine = myParticipants.First(x => x.ChatRoom_id == room.ID_ChatRoom);
            var roomMsgs = messages.Where(m => m.ChatRoom_id == room.ID_ChatRoom).ToList();
            var last = roomMsgs.FirstOrDefault();
            var unread = roomMsgs.Count(m => !m.Is_deleted && m.Sender_id != userId && (!mine.LastRead_at.HasValue || m.Sent_at > mine.LastRead_at.Value));
            var roomKind = NormalizeRoomKind(room.ChatRoomType?.Name);
            int? peerUserId = null;
            if (roomKind == "direct")
            {
                peerUserId = room.Participants.FirstOrDefault(p => p.Is_active && p.User_id != userId)?.User_id;
            }
            var name = room.Name;
            if (roomKind == "direct")
            {
                name = null;
            }
            else if (roomKind == "company")
            {
                name = "Чат компании";
            }

            if (string.IsNullOrWhiteSpace(name) && peerUserId.HasValue)
            {
                var peer = users.FirstOrDefault(u => u.ID_User == peerUserId.Value);
                if (peer != null)
                    name = $"{peer.FName} {peer.Name}".Trim();
            }

            list.Add(new
            {
                chatRoomId = room.ID_ChatRoom,
                name = name ?? $"Чат #{room.ID_ChatRoom}",
                roomKind,
                peerUserId,
                lastMessageText = last == null ? null : (last.Is_deleted ? "[deleted]" : DecryptSafe(last.MessageText)),
                lastMessageAt = room.LastMessage_at ?? last?.Sent_at ?? room.Created_at,
                unreadCount = unread
            });
        }

        return new OkObjectResult(list);
    }

    public async Task<IActionResult> GetOrCreateCompanyRoomAsync(int companyId, int userId)
    {
        var typeId = await ResolveRoomTypeIdAsync("company", "Чат компании");
        var room = await _context.ChatRooms
            .Include(r => r.ChatRoomType)
            .Where(r => r.Company_id == companyId && r.Order_id == null)
            .OrderBy(r => r.ID_ChatRoom)
            .FirstOrDefaultAsync(r =>
                r.ChatRoomType_id == typeId ||
                NormalizeRoomKind(r.ChatRoomType!.Name) == "company" ||
                (r.Name != null && (r.Name.Contains("чат компании", StringComparison.OrdinalIgnoreCase) || r.Name.Contains("общий чат", StringComparison.OrdinalIgnoreCase))));
        if (room == null)
        {
            room = new ChatRoom
            {
                Company_id = companyId,
                ChatRoomType_id = typeId,
                Name = "Чат компании",
                Created_at = DateTime.UtcNow
            };
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
        }
        else if (!string.Equals(room.Name, "Чат компании", StringComparison.Ordinal))
        {
            room.Name = "Чат компании";
            await _context.SaveChangesAsync();
        }

        await EnsureParticipantAsync(room.ID_ChatRoom, userId);
        return new OkObjectResult(new { roomId = room.ID_ChatRoom, roomName = room.Name });
    }

    public async Task<IActionResult> CreateOrGetDirectRoomAsync(int companyId, int userId, int peerUserId)
    {
        if (peerUserId <= 0 || peerUserId == userId)
            return new BadRequestObjectResult(new { message = "Некорректный собеседник." });

        var typeId = await ResolveRoomTypeIdAsync("direct", "Личный чат между двумя пользователями");

        var myRoomIds = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == userId && p.Is_active)
            .Select(p => p.ChatRoom_id)
            .ToListAsync();
        var existing = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == peerUserId && p.Is_active && myRoomIds.Contains(p.ChatRoom_id))
            .Join(_context.ChatRooms.AsNoTracking().Where(r => r.Company_id == companyId && r.ChatRoomType_id == typeId),
                p => p.ChatRoom_id, r => r.ID_ChatRoom, (p, r) => r)
            .FirstOrDefaultAsync();

        ChatRoom room;
        if (existing != null)
        {
            room = existing;
        }
        else
        {
            room = new ChatRoom
            {
                Company_id = companyId,
                ChatRoomType_id = typeId,
                Name = null,
                Created_at = DateTime.UtcNow
            };
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
        }

        await EnsureParticipantAsync(room.ID_ChatRoom, userId);
        await EnsureParticipantAsync(room.ID_ChatRoom, peerUserId);
        return new OkObjectResult(new { roomId = room.ID_ChatRoom, roomName = room.Name });
    }

    public async Task<IActionResult> GetQuickReplyTemplatesAsync(int companyId, int userId, string? category = null, string? search = null)
    {
        var query = _context.ChatQuickReplyTemplates.AsNoTracking()
            .Where(t => t.Company_id == companyId && (t.User_id == userId || t.User_id == 0) && t.Is_active);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(s) || t.Content.ToLower().Contains(s));
        }

        var list = await query
            .OrderBy(t => t.Category)
            .ThenByDescending(t => t.Created_at)
            .Select(t => new
            {
                id = t.ID_ChatQuickReplyTemplate,
                userId = t.User_id,
                category = t.Category,
                title = t.Title,
                content = t.Content,
                isActive = t.Is_active,
                createdAt = t.Created_at
            })
            .ToListAsync();
        return new OkObjectResult(list);
    }

    public async Task<IActionResult> UpsertQuickReplyTemplateAsync(int companyId, int userId, UpsertChatQuickReplyTemplateRequest request)
    {
        if (request == null)
            return new BadRequestObjectResult(new { message = "Пустой запрос." });

        var category = string.IsNullOrWhiteSpace(request.Category) ? "other" : request.Category.Trim();
        var title = (request.Title ?? string.Empty).Trim();
        var content = (request.Content ?? string.Empty).Trim();
        if (title.Length == 0 || content.Length == 0)
            return new BadRequestObjectResult(new { message = "Title/Content обязательны." });

        ChatQuickReplyTemplate entity;
        if (request.TemplateId.HasValue && request.TemplateId.Value > 0)
        {
            entity = await _context.ChatQuickReplyTemplates
                .FirstOrDefaultAsync(t => t.ID_ChatQuickReplyTemplate == request.TemplateId.Value && t.Company_id == companyId && t.User_id == userId);
            if (entity == null)
                return new NotFoundResult();
        }
        else
        {
            entity = new ChatQuickReplyTemplate
            {
                Company_id = companyId,
                User_id = userId,
                Created_at = DateTime.UtcNow
            };
            _context.ChatQuickReplyTemplates.Add(entity);
        }

        entity.Category = category;
        entity.Title = title;
        entity.Content = content;
        entity.Is_active = request.IsActive;
        await _context.SaveChangesAsync();
        return new OkObjectResult(new { id = entity.ID_ChatQuickReplyTemplate });
    }

    public async Task<IActionResult> DeleteQuickReplyTemplateAsync(int companyId, int userId, int templateId)
    {
        var entity = await _context.ChatQuickReplyTemplates
            .FirstOrDefaultAsync(t => t.ID_ChatQuickReplyTemplate == templateId && t.Company_id == companyId && t.User_id == userId);
        if (entity == null)
            return new NotFoundResult();
        _context.ChatQuickReplyTemplates.Remove(entity);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    private async Task EnsureParticipantAsync(int chatRoomId, int userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == userId);
        if (participant == null)
        {
            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatRoom_id = chatRoomId,
                User_id = userId,
                Joined_at = DateTime.UtcNow,
                Is_active = true
            });
        }
        else if (!participant.Is_active)
        {
            participant.Is_active = true;
            participant.Left_at = null;
        }
        await _context.SaveChangesAsync();
    }

    private async Task<int> ResolveRoomTypeIdAsync(string code, string description)
    {
        var existing = await _context.ChatRoomTypes
            .FirstOrDefaultAsync(t =>
                t.Name == code ||
                t.Name.ToLower() == code.ToLower() ||
                NormalizeRoomKind(t.Name) == NormalizeRoomKind(code));
        if (existing != null)
            return existing.ID_ChatRoomType;

        var created = new ChatRoomType
        {
            Name = code,
            Description = description
        };
        _context.ChatRoomTypes.Add(created);
        await _context.SaveChangesAsync();
        return created.ID_ChatRoomType;
    }

    private static string NormalizeRoomKind(string? roomTypeName)
    {
        if (string.IsNullOrWhiteSpace(roomTypeName))
            return "company";
        var v = roomTypeName.Trim().ToLowerInvariant();
        if (v.Contains("direct")) return "direct";
        if (v.Contains("order")) return "order";
        if (v.Contains("company") || v.Contains("group") || v.Contains("общ")) return "company";
        return v;
    }

    private string DecryptSafe(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;
        try
        {
            return _crypto.Decrypt(payload);
        }
        catch
        {
            return payload;
        }
    }

    private async Task BroadcastToRoomParticipantsAsync(int chatRoomId, string method, object payload)
    {
        await _hubContext.Clients.Group($"ChatRoom_{chatRoomId}").SendAsync(method, payload);

        var participantIds = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.ChatRoom_id == chatRoomId && p.Is_active)
            .Select(p => p.User_id)
            .Distinct()
            .ToListAsync();

        foreach (var userId in participantIds)
            await _hubContext.Clients.Group($"User_{userId}").SendAsync(method, payload);
    }
}
