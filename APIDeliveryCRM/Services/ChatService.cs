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
    private readonly INotificationService _notificationService;
    private readonly IUserPresenceService _presence;

    public ChatService(ContextDB context, IHubContext<ChatHub> hubContext, IChatMessageCryptoService crypto, INotificationService notificationService, IUserPresenceService presence)
    {
        _context = context;
        _hubContext = hubContext;
        _crypto = crypto;
        _notificationService = notificationService;
        _presence = presence;
    }

    public async Task<IActionResult> SendMessageAsync(int chatRoomId, int senderId, string? messageText, string? attachmentUrl = null, int? replyToMessageId = null, List<int>? mentionedUserIds = null)
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

        // Проверяем replyToMessageId
        if (replyToMessageId.HasValue)
        {
            var replyExists = await _context.ChatMessages.AsNoTracking()
                .AnyAsync(m => m.ID_ChatMessage == replyToMessageId.Value && m.ChatRoom_id == chatRoomId && !m.Is_deleted);
            if (!replyExists)
                return new BadRequestObjectResult(new { message = "Сообщение для ответа не найдено." });
        }

        var plainText = (messageText ?? string.Empty).Trim();
        var storedText = string.IsNullOrWhiteSpace(plainText) ? string.Empty : _crypto.Encrypt(plainText);

        // Сериализуем mentions
        string? mentionsJson = null;
        var validMentions = mentionedUserIds?.Where(id => id > 0).Distinct().ToList();
        if (validMentions?.Count > 0)
            mentionsJson = System.Text.Json.JsonSerializer.Serialize(validMentions);

        var msg = new ChatMessage
        {
            ChatRoom_id = chatRoomId,
            Sender_id = senderId,
            MessageText = storedText,
            AttachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl) ? null : attachmentUrl.Trim(),
            Sent_at = DateTime.UtcNow,
            Is_deleted = false,
            ReplyToMessage_id = replyToMessageId,
            MentionedUserIds = mentionsJson,
            DeliveryStatus = 0
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

        // Загружаем цитируемое сообщение для ответа
        object? replyPreview = null;
        if (replyToMessageId.HasValue)
        {
            var replyMsg = await _context.ChatMessages.AsNoTracking()
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.ID_ChatMessage == replyToMessageId.Value);
            if (replyMsg != null)
            {
                replyPreview = new
                {
                    id = replyMsg.ID_ChatMessage,
                    senderId = replyMsg.Sender_id,
                    senderName = ((replyMsg.Sender?.FName ?? "") + " " + (replyMsg.Sender?.Name ?? "")).Trim(),
                    messageText = replyMsg.Is_deleted ? "[deleted]" : DecryptSafe(replyMsg.MessageText)
                };
            }
        }

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
            isDeleted = msg.Is_deleted,
            replyToMessageId = msg.ReplyToMessage_id,
            replyPreview,
            mentionedUserIds = validMentions ?? new List<int>(),
            deliveryStatus = msg.DeliveryStatus
        };
        await BroadcastToRoomParticipantsAsync(chatRoomId, "ReceiveMessage", payload);

        // Push-уведомления участникам, которые не в сети
        await SendChatPushNotificationsAsync(chatRoomId, senderId, senderName, plainText, validMentions, msg.Sent_at);

        return new OkObjectResult(payload);
    }

    public async Task<IActionResult> GetMessagesAsync(int chatRoomId, int userId, int skip = 0, int take = 50)
    {
        if (take <= 0) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        if (!await IsActiveParticipantAsync(chatRoomId, userId))
            return new ForbidResult();

        var rows = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatRoom_id == chatRoomId)
            .Include(m => m.Sender)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .OrderByDescending(m => m.Sent_at)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Загружаем цитируемые сообщения одним запросом
        var replyIds = rows.Where(m => m.ReplyToMessage_id.HasValue).Select(m => m.ReplyToMessage_id!.Value).Distinct().ToList();
        var replyMessages = replyIds.Count > 0
            ? await _context.ChatMessages.AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => replyIds.Contains(m.ID_ChatMessage))
                .ToDictionaryAsync(m => m.ID_ChatMessage)
            : new Dictionary<int, ChatMessage>();

        var list = rows
            .Select(m =>
            {
                var mentions = new List<int>();
                if (!string.IsNullOrWhiteSpace(m.MentionedUserIds))
                {
                    try { mentions = System.Text.Json.JsonSerializer.Deserialize<List<int>>(m.MentionedUserIds) ?? new(); }
                    catch { }
                }

                object? replyPreview = null;
                if (m.ReplyToMessage_id.HasValue && replyMessages.TryGetValue(m.ReplyToMessage_id.Value, out var rm))
                {
                    replyPreview = new
                    {
                        id = rm.ID_ChatMessage,
                        senderId = rm.Sender_id,
                        senderName = ((rm.Sender?.FName ?? "") + " " + (rm.Sender?.Name ?? "")).Trim(),
                        messageText = rm.Is_deleted ? "[deleted]" : DecryptSafe(rm.MessageText)
                    };
                }

                var reactions = m.Reactions
                    .GroupBy(r => r.Emoji)
                    .Select(g => new
                    {
                        emoji = g.Key,
                        count = g.Count(),
                        userIds = g.Select(r => r.User_id).ToList()
                    }).ToList();

                return new
                {
                    ID_ChatMessage = m.ID_ChatMessage,
                    ChatRoom_id = m.ChatRoom_id,
                    Sender_id = m.Sender_id,
                    MessageText = m.Is_deleted ? "[deleted]" : DecryptSafe(m.MessageText),
                    SenderName = ((m.Sender?.FName ?? "") + " " + (m.Sender?.Name ?? "")).Trim(),
                    AttachmentUrl = m.AttachmentUrl,
                    Sent_at = m.Sent_at,
                    Edited_at = m.Edited_at,
                    Is_deleted = m.Is_deleted,
                    ReplyToMessageId = m.ReplyToMessage_id,
                    ReplyPreview = replyPreview,
                    MentionedUserIds = mentions,
                    DeliveryStatus = m.DeliveryStatus,
                    Reactions = reactions
                };
            })
            .OrderBy(m => m.Sent_at)
            .ToList<object>();

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
            return new ForbidResult();
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
        await _notificationService.MarkChatMessageNotificationsAsReadAsync(userId);
        return new OkResult();
    }

    public async Task<IActionResult> GetChatRoomsListAsync(int companyId, int userId)
    {
        var effectiveCompanyId = await GetUserCompanyIdAsync(userId) ?? companyId;
        if (effectiveCompanyId <= 0)
            effectiveCompanyId = companyId;

        await SyncUserChatAccessAsync(effectiveCompanyId, userId);

        var currentUser = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == userId);
        if (currentUser == null)
            return new OkObjectResult(new List<object>());

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
            .Where(r => roomIds.Contains(r.ID_ChatRoom))
            .ToListAsync();

        var orderIds = rooms.Where(r => r.Order_id.HasValue).Select(r => r.Order_id!.Value).Distinct().ToList();
        var ordersById = orderIds.Count == 0
            ? new Dictionary<int, Order>()
            : await _context.Orders.AsNoTracking()
                .Include(o => o.ClientProfile)
                .Where(o => orderIds.Contains(o.ID_Order))
                .ToDictionaryAsync(o => o.ID_Order);

        var relatedUserIds = rooms
            .SelectMany(r => r.Participants.Where(p => p.Is_active).Select(p => p.User_id))
            .Append(userId)
            .Distinct()
            .ToList();
        var usersById = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Company)
            .Where(u => relatedUserIds.Contains(u.ID_User))
            .ToDictionaryAsync(u => u.ID_User);

        rooms = rooms
            .Where(r => IsRoomAccessibleToUser(r, currentUser, usersById, ordersById))
            .ToList();
        roomIds = rooms.Select(r => r.ID_ChatRoom).ToArray();
        if (roomIds.Length == 0)
            return new OkObjectResult(new List<object>());

        myParticipants = myParticipants.Where(p => roomIds.Contains(p.ChatRoom_id)).ToList();

        var messages = await _context.ChatMessages.AsNoTracking()
            .Where(m => roomIds.Contains(m.ChatRoom_id))
            .OrderByDescending(m => m.Sent_at)
            .ToListAsync();

        var participantUserIds = rooms
            .SelectMany(r => r.Participants.Where(p => p.Is_active).Select(p => p.User_id))
            .Distinct()
            .ToList();

        var users = participantUserIds
            .Where(usersById.ContainsKey)
            .Select(id =>
            {
                var u = usersById[id];
                return new { u.ID_User, u.Name, u.FName, CompanyName = u.Company != null ? u.Company.Name : string.Empty };
            })
            .ToList();

        var viewerIsClient = currentUser.Role?.Name == "Клиент";

        var list = new List<RoomListBuildRow>();
        foreach (var room in rooms.OrderByDescending(r => r.LastMessage_at ?? r.Created_at))
        {
            var mine = myParticipants.First(x => x.ChatRoom_id == room.ID_ChatRoom);
            var roomMsgs = messages.Where(m => m.ChatRoom_id == room.ID_ChatRoom).ToList();
            var last = roomMsgs.OrderByDescending(m => m.Sent_at).FirstOrDefault();
            var unread = roomMsgs.Count(m => !m.Is_deleted && m.Sender_id != userId && (!mine.LastRead_at.HasValue || m.Sent_at > mine.LastRead_at.Value));
            var roomKind = NormalizeRoomKind(room.ChatRoomType?.Name);
            if (viewerIsClient && roomKind == "company")
                continue;
            // Пустые личные/заказные комнаты не показываем — чат сохраняется только после первого сообщения.
            if (last == null && (roomKind == "direct" || roomKind == "order"))
                continue;

            var peerUserId = ResolvePeerUserId(room, roomKind, userId, roomMsgs);
            var name = room.Name;
            if (roomKind == "direct")
                name = null;
            else if (roomKind == "company")
                name = "Чат компании";

            if (string.IsNullOrWhiteSpace(name) && peerUserId.HasValue)
            {
                var peer = users.FirstOrDefault(u => u.ID_User == peerUserId.Value);
                if (peer != null)
                    name = FormatDirectChatDisplayName(peer.CompanyName, peer.Name, peer.FName, viewerIsClient);
            }

            var sortAt = room.LastMessage_at ?? last?.Sent_at ?? room.Created_at;
            list.Add(new RoomListBuildRow
            {
                ChatRoomId = room.ID_ChatRoom,
                Name = name ?? $"Чат #{room.ID_ChatRoom}",
                RoomKind = roomKind,
                PeerUserId = peerUserId,
                LastMessageText = last == null ? null : (last.Is_deleted ? "[deleted]" : DecryptSafe(last.MessageText)),
                LastMessageAt = sortAt,
                UnreadCount = unread,
                DedupKey = BuildRoomListDedupKey(roomKind, room, peerUserId)
            });
        }

        var merged = new Dictionary<string, RoomListBuildRow>(StringComparer.Ordinal);
        foreach (var entry in list.OrderByDescending(x => x.LastMessageAt))
        {
            if (!merged.TryGetValue(entry.DedupKey, out var prev))
            {
                merged[entry.DedupKey] = entry;
                continue;
            }

            if (entry.LastMessageAt > prev.LastMessageAt)
            {
                entry.UnreadCount += prev.UnreadCount;
                merged[entry.DedupKey] = entry;
            }
            else
            {
                prev.UnreadCount += entry.UnreadCount;
                merged[entry.DedupKey] = prev;
            }
        }

        var result = merged.Values
            .OrderByDescending(x => x.LastMessageAt)
            .Select(x => (object)new
            {
                chatRoomId = x.ChatRoomId,
                name = x.Name,
                roomKind = x.RoomKind,
                peerUserId = x.PeerUserId,
                lastMessageText = x.LastMessageText,
                lastMessageAt = x.LastMessageAt,
                unreadCount = x.UnreadCount
            })
            .ToList();

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetOrCreateCompanyRoomAsync(int companyId, int userId)
    {
        if (await IsClientUserAsync(userId))
            return new ForbidResult();

        var room = await EnsureCompanyRoomEntityAsync(companyId);
        await EnsureParticipantAsync(room.ID_ChatRoom, userId);
        return new OkObjectResult(new { roomId = room.ID_ChatRoom, roomName = room.Name });
    }

    public async Task<IActionResult> CreateOrGetDirectRoomAsync(int companyId, int userId, int peerUserId)
    {
        if (peerUserId <= 0 || peerUserId == userId)
        {
            return new BadRequestObjectResult(new { message = "Некорректный собеседник." });
        }

        var currentUser = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == userId);
        var peerUser = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == peerUserId);
        if (currentUser == null || peerUser == null)
            return new NotFoundObjectResult(new { message = "Пользователь не найден." });
        if (!IsAllowedDirectPeer(currentUser.Role?.Name, peerUser.Role?.Name))
            return new ForbidResult();
        if (currentUser.Role?.Name != "Клиент" && peerUser.Role?.Name != "Клиент" && currentUser.Company_id != peerUser.Company_id)
            return new ForbidResult();

        var typeId = await ResolveRoomTypeIdAsync("direct", "Личный чат между двумя пользователями");

        var trackedExisting = await FindDirectRoomBetweenUsersAsync(userId, peerUserId, companyId, tracked: true);
        ChatRoom room;
        if (trackedExisting != null)
        {
            room = trackedExisting;
            if (room.Company_id <= 0)
                room.Company_id = companyId;
            if (NormalizeRoomKind(room.ChatRoomType?.Name) != "direct")
            {
                room.ChatRoomType_id = typeId;
                room.Name = null;
            }
            else if (!string.IsNullOrWhiteSpace(room.Name) &&
                     (TryParseLegacyDirectName(room.Name, userId, out _) || !room.Name.StartsWith("ЛС", StringComparison.OrdinalIgnoreCase)))
            {
                room.Name = null;
            }
            await _context.SaveChangesAsync();
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
        await ConsolidateDuplicateDirectRoomsAsync(userId);
        return new OkObjectResult(new { roomId = room.ID_ChatRoom, roomName = room.Name });
    }

    public async Task<IActionResult> GetOrCreateOrderRoomAsync(int orderId, int userId, int? peerUserId = null)
    {
        if (orderId <= 0)
            return new BadRequestObjectResult(new { message = "Некорректный заказ." });

        var currentUser = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == userId);
        if (currentUser == null)
            return new ForbidResult();

        var order = await _context.Orders.AsNoTracking()
            .Include(o => o.ClientProfile)
            .FirstOrDefaultAsync(o => o.ID_Order == orderId);
        if (order == null)
            return new NotFoundObjectResult(new { message = "Заказ не найден." });

        var isClient = currentUser.Role?.Name == "Клиент";
        if (isClient && order.ClientProfile?.User_id != userId)
            return new ForbidResult();
        if (!isClient && currentUser.Company_id != order.Company_id)
            return new ForbidResult();

        var participantIds = new List<int> { userId };
        if (order.ClientProfile?.User_id > 0)
            participantIds.Add(order.ClientProfile.User_id);

        if (peerUserId.HasValue && peerUserId.Value > 0 && peerUserId.Value != userId)
        {
            var peer = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ID_User == peerUserId.Value);
            if (peer == null)
                return new NotFoundObjectResult(new { message = "Собеседник не найден." });
            if (peer.Company_id != order.Company_id || peer.Role?.Name == "Клиент")
                return new ForbidResult();
            participantIds.Add(peer.ID_User);
        }
        else
        {
            var employeeId = await _context.Users.AsNoTracking()
                .Where(u => u.Company_id == order.Company_id &&
                            (u.Role.Name == "Менеджер" || u.Role.Name == "Логист" || u.Role.Name == "Админ" || u.Role.Name == "Администратор"))
                .OrderBy(u => u.Role.Name == "Менеджер" ? 0 : u.Role.Name == "Логист" ? 1 : 2)
                .Select(u => u.ID_User)
                .FirstOrDefaultAsync();
            if (employeeId > 0)
                participantIds.Add(employeeId);
        }

        participantIds = participantIds.Where(id => id > 0).Distinct().ToList();
        if (participantIds.Count < 2)
            return new BadRequestObjectResult(new { message = "Не найден сотрудник для чата по заказу." });

        var typeId = await ResolveRoomTypeIdAsync("order", "Комната по заказу");
        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(r => r.Order_id == order.ID_Order && r.ChatRoomType_id == typeId);
        if (room == null)
        {
            room = new ChatRoom
            {
                Company_id = order.Company_id,
                ChatRoomType_id = typeId,
                Order_id = order.ID_Order,
                Name = $"Заказ #{order.Order_Number}",
                Created_at = DateTime.UtcNow
            };
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
        }

        foreach (var participantId in participantIds)
            await EnsureParticipantAsync(room.ID_ChatRoom, participantId);

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

    public async Task<IActionResult> ModerateDeleteMessageAsync(int messageId, int moderatorUserId)
    {
        // Проверяем, что модератор — менеджер/администратор
        var moderator = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == moderatorUserId);
        if (moderator == null)
            return new ForbidResult();

        var allowedRoles = new[] { "Менеджер", "Администратор", "Админ" };
        if (!allowedRoles.Contains(moderator.Role?.Name))
            return new ForbidResult();

        var msg = await _context.ChatMessages.FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();

        // Проверяем, что модератор из той же компании, что и комната
        var room = await _context.ChatRooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID_ChatRoom == msg.ChatRoom_id);
        if (room == null || room.Company_id != moderator.Company_id)
            return new ForbidResult();

        msg.Is_deleted = true;
        msg.MessageText = string.Empty;
        msg.AttachmentUrl = null;
        msg.Edited_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(msg.ChatRoom_id, "MessageDeleted",
            new { messageId, chatRoomId = msg.ChatRoom_id, moderatedBy = moderatorUserId });
        return new OkResult();
    }

    // ─── Управление участниками ───────────────────────────────────────────────

    public async Task<IActionResult> GetParticipantsAsync(int chatRoomId, int requestingUserId)
    {
        if (!await IsActiveParticipantAsync(chatRoomId, requestingUserId))
            return new ForbidResult();

        var participants = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.ChatRoom_id == chatRoomId && p.Is_active)
            .Include(p => p.User).ThenInclude(u => u.Role)
            .ToListAsync();

        var result = participants.Select(p => new
        {
            userId = p.User_id,
            name = ((p.User?.FName ?? "") + " " + (p.User?.Name ?? "")).Trim(),
            role = p.User?.Role?.Name,
            joinedAt = p.Joined_at,
            lastReadAt = p.LastRead_at
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> AddParticipantAsync(int chatRoomId, int targetUserId, int requestingUserId)
    {
        // Только менеджер/администратор может добавлять участников
        var requester = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == requestingUserId);
        if (requester == null)
            return new ForbidResult();

        var allowedRoles = new[] { "Менеджер", "Администратор", "Админ" };
        if (!allowedRoles.Contains(requester.Role?.Name))
            return new ForbidResult();

        var room = await _context.ChatRooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID_ChatRoom == chatRoomId);
        if (room == null)
            return new NotFoundObjectResult(new { message = "Чат не найден." });

        if (room.Company_id != requester.Company_id)
            return new ForbidResult();

        var targetUser = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ID_User == targetUserId);
        if (targetUser == null)
            return new NotFoundObjectResult(new { message = "Пользователь не найден." });

        await EnsureParticipantAsync(chatRoomId, targetUserId);

        await BroadcastToRoomParticipantsAsync(chatRoomId, "ParticipantAdded",
            new { chatRoomId, userId = targetUserId, addedBy = requestingUserId });

        return new OkResult();
    }

    public async Task<IActionResult> RemoveParticipantAsync(int chatRoomId, int targetUserId, int requestingUserId)
    {
        var requester = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == requestingUserId);
        if (requester == null)
            return new ForbidResult();

        var allowedRoles = new[] { "Менеджер", "Администратор", "Админ" };
        // Пользователь может удалить сам себя, или менеджер/администратор — любого
        if (targetUserId != requestingUserId && !allowedRoles.Contains(requester.Role?.Name))
            return new ForbidResult();

        var room = await _context.ChatRooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID_ChatRoom == chatRoomId);
        if (room == null)
            return new NotFoundObjectResult(new { message = "Чат не найден." });

        if (targetUserId != requestingUserId && room.Company_id != requester.Company_id)
            return new ForbidResult();

        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == targetUserId && p.Is_active);
        if (participant == null)
            return new NotFoundObjectResult(new { message = "Участник не найден." });

        participant.Is_active = false;
        participant.Left_at = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(chatRoomId, "ParticipantRemoved",
            new { chatRoomId, userId = targetUserId, removedBy = requestingUserId });

        return new OkResult();
    }

    // ─── Поиск по сообщениям ─────────────────────────────────────────────────

    public async Task<IActionResult> SearchMessagesAsync(int chatRoomId, int userId, string searchText, int skip = 0, int take = 50)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new BadRequestObjectResult(new { message = "Поисковый запрос не может быть пустым." });

        if (!await IsActiveParticipantAsync(chatRoomId, userId))
            return new ForbidResult();

        if (take <= 0) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        // Загружаем все незашифрованные сообщения и фильтруем в памяти (т.к. текст зашифрован)
        var allMessages = await _context.ChatMessages.AsNoTracking()
            .Where(m => m.ChatRoom_id == chatRoomId && !m.Is_deleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.Sent_at)
            .ToListAsync();

        var search = searchText.Trim().ToLowerInvariant();
        var matched = allMessages
            .Where(m => DecryptSafe(m.MessageText).ToLowerInvariant().Contains(search))
            .Skip(skip)
            .Take(take)
            .Select(m => new
            {
                id = m.ID_ChatMessage,
                chatRoomId = m.ChatRoom_id,
                senderId = m.Sender_id,
                senderName = ((m.Sender?.FName ?? "") + " " + (m.Sender?.Name ?? "")).Trim(),
                messageText = DecryptSafe(m.MessageText),
                sentAt = m.Sent_at,
                editedAt = m.Edited_at
            })
            .ToList();

        return new OkObjectResult(matched);
    }

    // ─── Реакции ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> AddReactionAsync(int messageId, int userId, string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            return new BadRequestObjectResult(new { message = "Emoji не может быть пустым." });

        var msg = await _context.ChatMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId && !m.Is_deleted);
        if (msg == null)
            return new NotFoundResult();

        if (!await IsActiveParticipantAsync(msg.ChatRoom_id, userId))
            return new ForbidResult();

        var emojiClean = emoji.Trim()[..Math.Min(emoji.Trim().Length, 50)];

        var existing = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.ChatMessage_id == messageId && r.User_id == userId && r.Emoji == emojiClean);
        if (existing != null)
            return new OkResult(); // уже есть

        _context.MessageReactions.Add(new MessageReaction
        {
            ChatMessage_id = messageId,
            User_id = userId,
            Emoji = emojiClean,
            Created_at = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(msg.ChatRoom_id, "ReactionAdded",
            new { messageId, chatRoomId = msg.ChatRoom_id, userId, emoji = emojiClean });

        return new OkResult();
    }

    public async Task<IActionResult> RemoveReactionAsync(int messageId, int userId, string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            return new BadRequestObjectResult(new { message = "Emoji не может быть пустым." });

        var msg = await _context.ChatMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();

        if (!await IsActiveParticipantAsync(msg.ChatRoom_id, userId))
            return new ForbidResult();

        var emojiClean = emoji.Trim()[..Math.Min(emoji.Trim().Length, 50)];
        var reaction = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.ChatMessage_id == messageId && r.User_id == userId && r.Emoji == emojiClean);
        if (reaction == null)
            return new NotFoundResult();

        _context.MessageReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        await BroadcastToRoomParticipantsAsync(msg.ChatRoom_id, "ReactionRemoved",
            new { messageId, chatRoomId = msg.ChatRoom_id, userId, emoji = emojiClean });

        return new OkResult();
    }

    public async Task<IActionResult> GetReactionsAsync(int messageId, int userId)
    {
        var msg = await _context.ChatMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ID_ChatMessage == messageId);
        if (msg == null)
            return new NotFoundResult();

        if (!await IsActiveParticipantAsync(msg.ChatRoom_id, userId))
            return new ForbidResult();

        var reactions = await _context.MessageReactions.AsNoTracking()
            .Where(r => r.ChatMessage_id == messageId)
            .Include(r => r.User)
            .ToListAsync();

        var grouped = reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new
            {
                emoji = g.Key,
                count = g.Count(),
                users = g.Select(r => new
                {
                    userId = r.User_id,
                    name = ((r.User?.FName ?? "") + " " + (r.User?.Name ?? "")).Trim()
                }).ToList()
            });

        return new OkObjectResult(grouped);
    }

    // ─── Push-уведомления при новом сообщении ────────────────────────────────

    private async Task SendChatPushNotificationsAsync(int chatRoomId, int senderId, string? senderName, string messageText, List<int>? mentionedUserIds, DateTime messageSentAt)
    {
        var participants = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.ChatRoom_id == chatRoomId && p.Is_active && p.User_id != senderId)
            .Select(p => new { p.User_id, p.LastRead_at })
            .ToListAsync();

        if (participants.Count == 0)
            return;

        var displayName = string.IsNullOrWhiteSpace(senderName) ? $"Пользователь #{senderId}" : senderName;
        var preview = messageText.Length > 100 ? messageText[..100] + "…" : messageText;
        var body = string.IsNullOrWhiteSpace(preview) ? "(вложение)" : preview;

        foreach (var participant in participants)
        {
            var recipientId = participant.User_id;
            if (_presence.IsViewingRoom(recipientId, chatRoomId))
                continue;
            if (participant.LastRead_at.HasValue && participant.LastRead_at.Value >= messageSentAt)
                continue;

            var isMentioned = mentionedUserIds?.Contains(recipientId) == true;
            var title = isMentioned ? $"@упоминание от {displayName}" : $"Новое сообщение от {displayName}";

            await _hubContext.Clients.Group($"User_{recipientId}").SendAsync("NotificationReceived", new
            {
                id = 0,
                title,
                message = body,
                chatRoomId,
                isChat = true
            });
        }
    }

    public async Task<int?> GetUserCompanyIdAsync(int userId)
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.ID_User == userId)
            .Select(u => (int?)u.Company_id)
            .FirstOrDefaultAsync();
    }

    /// <summary>Поддерживает корпоративный чат и убирает лишние/дублирующие участия.</summary>
    private async Task SyncUserChatAccessAsync(int companyId, int userId)
    {
        if (!await IsClientUserAsync(userId))
            await EnsureCompanyRoomParticipantAsync(companyId, userId);

        await ConsolidateDuplicateDirectRoomsAsync(userId);
        await PruneUnauthorizedParticipationsAsync(userId);
    }

    /// <summary>Деактивирует участие в комнатах, к которым пользователь не должен иметь доступ.</summary>
    private async Task PruneUnauthorizedParticipationsAsync(int userId)
    {
        var currentUser = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ID_User == userId);
        if (currentUser == null)
            return;

        var participations = await _context.ChatParticipants
            .Include(p => p.ChatRoom)
                .ThenInclude(r => r.ChatRoomType)
            .Include(p => p.ChatRoom)
                .ThenInclude(r => r.Participants)
            .Where(p => p.User_id == userId && p.Is_active)
            .ToListAsync();
        if (participations.Count == 0)
            return;

        var orderIds = participations
            .Where(p => p.ChatRoom.Order_id.HasValue)
            .Select(p => p.ChatRoom.Order_id!.Value)
            .Distinct()
            .ToList();
        var ordersById = orderIds.Count == 0
            ? new Dictionary<int, Order>()
            : await _context.Orders.AsNoTracking()
                .Include(o => o.ClientProfile)
                .Where(o => orderIds.Contains(o.ID_Order))
                .ToDictionaryAsync(o => o.ID_Order);

        var relatedUserIds = participations
            .SelectMany(p => p.ChatRoom.Participants.Where(x => x.Is_active).Select(x => x.User_id))
            .Append(userId)
            .Distinct()
            .ToList();
        var usersById = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Company)
            .Where(u => relatedUserIds.Contains(u.ID_User))
            .ToDictionaryAsync(u => u.ID_User);

        var changed = false;
        foreach (var part in participations)
        {
            if (IsRoomAccessibleToUser(part.ChatRoom, currentUser, usersById, ordersById))
                continue;

            part.Is_active = false;
            part.Left_at = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
            await _context.SaveChangesAsync();
    }

    private static bool IsRoomAccessibleToUser(
        ChatRoom room,
        User currentUser,
        IReadOnlyDictionary<int, User> usersById,
        IReadOnlyDictionary<int, Order> ordersById)
    {
        var userId = currentUser.ID_User;
        var userRole = currentUser.Role?.Name ?? string.Empty;
        var userCompanyId = currentUser.Company_id;
        var kind = NormalizeRoomKind(room.ChatRoomType?.Name);
        var activeIds = room.Participants.Where(p => p.Is_active).Select(p => p.User_id).ToHashSet();
        if (!activeIds.Contains(userId))
            return false;

        if (kind == "company")
        {
            if (userRole == "Клиент")
                return false;
            return userCompanyId <= 0 || room.Company_id <= 0 || room.Company_id == userCompanyId;
        }

        if (kind == "order")
        {
            if (!room.Order_id.HasValue || !ordersById.TryGetValue(room.Order_id.Value, out var order))
                return false;
            if (userRole == "Клиент")
                return order.ClientProfile?.User_id == userId;
            return userCompanyId == order.Company_id;
        }

        var peerIds = activeIds.Where(id => id != userId).Distinct().ToList();
        if (peerIds.Count != 1)
            return false;

        if (!usersById.TryGetValue(peerIds[0], out var peer))
            return false;
        if (!IsAllowedDirectPeer(userRole, peer.Role?.Name))
            return false;
        if (userRole != "Клиент" && peer.Role?.Name != "Клиент" && userCompanyId != peer.Company_id)
            return false;

        return kind == "direct" || kind != "company";
    }

    private async Task EnsureCompanyRoomParticipantAsync(int companyId, int userId)
    {
        if (await IsClientUserAsync(userId))
            return;

        var room = await EnsureCompanyRoomEntityAsync(companyId);
        await EnsureParticipantAsync(room.ID_ChatRoom, userId);
    }

    /// <summary>Находит или создаёт общий чат компании (фильтрация типа — в памяти, EF не переводит NormalizeRoomKind).</summary>
    private async Task<ChatRoom> EnsureCompanyRoomEntityAsync(int companyId)
    {
        var typeId = await ResolveRoomTypeIdAsync("company", "Чат компании");
        var room = await FindExistingCompanyRoomAsync(companyId, typeId);
        if (room != null)
        {
            var changed = false;
            if (room.Company_id <= 0)
            {
                room.Company_id = companyId;
                changed = true;
            }
            if (!string.Equals(room.Name, "Чат компании", StringComparison.Ordinal))
            {
                room.Name = "Чат компании";
                changed = true;
            }
            if (changed)
                await _context.SaveChangesAsync();
            return room;
        }

        room = new ChatRoom
        {
            Company_id = companyId,
            ChatRoomType_id = typeId,
            Name = "Чат компании",
            Created_at = DateTime.UtcNow
        };
        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    private async Task<ChatRoom?> FindExistingCompanyRoomAsync(int companyId, int companyTypeId)
    {
        var candidates = await _context.ChatRooms
            .Include(r => r.ChatRoomType)
            .Where(r => r.Order_id == null && (r.Company_id == companyId || r.Company_id <= 0))
            .ToListAsync();

        return candidates
            .OrderBy(r => r.Company_id == companyId ? 0 : 1)
            .ThenBy(r => r.ID_ChatRoom)
            .FirstOrDefault(r => IsCompanyRoomCandidate(r, companyTypeId));
    }

    private static bool IsCompanyRoomCandidate(ChatRoom room, int companyTypeId) =>
        room.ChatRoomType_id == companyTypeId
        || NormalizeRoomKind(room.ChatRoomType?.Name) == "company"
        || (!string.IsNullOrWhiteSpace(room.Name)
            && (room.Name.Contains("чат компании", StringComparison.OrdinalIgnoreCase)
                || room.Name.Contains("общий чат", StringComparison.OrdinalIgnoreCase)));

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

    private async Task<bool> IsActiveParticipantAsync(int chatRoomId, int userId)
    {
        return await _context.ChatParticipants.AsNoTracking()
            .AnyAsync(p => p.ChatRoom_id == chatRoomId && p.User_id == userId && p.Is_active);
    }

    private async Task<bool> IsClientUserAsync(int userId)
    {
        return await _context.Users.AsNoTracking()
            .AnyAsync(u => u.ID_User == userId && u.Role != null && u.Role.Name == "Клиент");
    }

    private static bool IsAllowedDirectPeer(string? currentRole, string? peerRole)
    {
        if (string.IsNullOrWhiteSpace(currentRole) || string.IsNullOrWhiteSpace(peerRole))
            return false;
        if (currentRole == "Клиент")
            return peerRole != "Клиент";
        if (peerRole == "Клиент")
            return true;
        return true;
    }

    private async Task<int> ResolveRoomTypeIdAsync(string code, string description)
    {
        var codeLower = code.ToLowerInvariant();
        var normalizedCode = NormalizeRoomKind(code);

        // Сначала — SQL-транслируемые проверки.
        var existing = await _context.ChatRoomTypes
            .FirstOrDefaultAsync(t =>
                t.Name == code ||
                t.Name.ToLower() == codeLower);

        // Затем — нормализация по нашим правилам в памяти (EF не умеет транслировать NormalizeRoomKind).
        if (existing == null)
        {
            var allTypes = await _context.ChatRoomTypes.ToListAsync();
            existing = allTypes.FirstOrDefault(t => NormalizeRoomKind(t.Name) == normalizedCode);
        }
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

    /// <summary>
    /// Личный чат: для клиента — «Компания: Фамилия Имя»; для сотрудников — только «Фамилия Имя».
    /// </summary>
    private static string FormatDirectChatDisplayName(
        string? companyName,
        string? surname,
        string? givenName,
        bool includeCompanyPrefix)
    {
        var company = (companyName ?? string.Empty).Trim();
        var last = (surname ?? string.Empty).Trim();
        var first = (givenName ?? string.Empty).Trim();
        var person = string.Join(" ", new[] { last, first }.Where(p => !string.IsNullOrEmpty(p)));
        if (!includeCompanyPrefix)
            return string.IsNullOrEmpty(person) ? "Сотрудник" : person;
        if (string.IsNullOrEmpty(company))
            return string.IsNullOrEmpty(person) ? "Сотрудник" : person;
        if (string.IsNullOrEmpty(person))
            return company;
        return $"{company}: {person}";
    }

    private static string NormalizeRoomKind(string? roomTypeName)
    {
        if (string.IsNullOrWhiteSpace(roomTypeName))
            return "company";
        var v = roomTypeName.Trim().ToLowerInvariant();
        if (v.Contains("direct") || v.Contains("лич") || v.StartsWith("лс") || v == "dm") return "direct";
        if (v.Contains("order")) return "order";
        if (v.Contains("company") || v.Contains("group") || v.Contains("общ")) return "company";
        return v;
    }

    /// <summary>Общая личная комната с собеседником (без привязки к одному ChatRoomType_id / Company_id).</summary>
    private async Task<ChatRoom?> FindDirectRoomBetweenUsersAsync(int userId, int peerUserId, int companyId, bool tracked)
    {
        var myRoomIds = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == userId && p.Is_active)
            .Select(p => p.ChatRoom_id)
            .ToListAsync();
        if (myRoomIds.Count == 0)
            return null;

        var sharedIds = await _context.ChatParticipants.AsNoTracking()
            .Where(p => p.User_id == peerUserId && p.Is_active && myRoomIds.Contains(p.ChatRoom_id))
            .Select(p => p.ChatRoom_id)
            .Distinct()
            .ToListAsync();
        if (sharedIds.Count == 0)
            return null;

        IQueryable<ChatRoom> query = tracked
            ? _context.ChatRooms.Include(r => r.ChatRoomType).Include(r => r.Participants)
            : _context.ChatRooms.AsNoTracking().Include(r => r.ChatRoomType).Include(r => r.Participants);

        var candidates = await query
            .Where(r => sharedIds.Contains(r.ID_ChatRoom) && r.Order_id == null)
            .ToListAsync();

        var ordered = candidates
            .Where(r => IsDirectRoomCandidate(r, userId, peerUserId))
            .Where(r => companyId <= 0 || r.Company_id <= 0 || r.Company_id == companyId)
            .OrderByDescending(r => r.LastMessage_at ?? r.Created_at)
            .ThenByDescending(r => r.ID_ChatRoom)
            .ToList();

        foreach (var candidate in ordered)
        {
            if (await RoomHasAnyMessageAsync(candidate.ID_ChatRoom))
                return candidate;

            await DeleteRoomIfEmptyAsync(candidate.ID_ChatRoom);
        }

        return null;
    }

    private async Task<bool> RoomHasAnyMessageAsync(int chatRoomId)
        => await _context.ChatMessages.AnyAsync(m => m.ChatRoom_id == chatRoomId && !m.Is_deleted);

    private async Task DeleteRoomIfEmptyAsync(int chatRoomId)
    {
        if (await RoomHasAnyMessageAsync(chatRoomId))
            return;

        var room = await _context.ChatRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.ID_ChatRoom == chatRoomId);
        if (room == null)
            return;

        _context.ChatParticipants.RemoveRange(room.Participants);
        _context.ChatRooms.Remove(room);
        await _context.SaveChangesAsync();
    }

    private static bool IsDirectRoomCandidate(ChatRoom room, int userId, int peerUserId)
    {
        var kind = NormalizeRoomKind(room.ChatRoomType?.Name);
        if (kind == "company" || kind == "order")
            return false;
        if (kind == "direct")
            return true;
        if (TryParseLegacyDirectName(room.Name, userId, out var legacyPeer) && legacyPeer == peerUserId)
            return true;

        var activePeers = room.Participants
            .Where(p => p.Is_active && p.User_id != userId)
            .Select(p => p.User_id)
            .Distinct()
            .ToList();
        return activePeers.Count == 1 && activePeers[0] == peerUserId;
    }

    private static int? ResolvePeerUserId(ChatRoom room, string roomKind, int userId, List<ChatMessage> roomMsgs)
    {
        if (roomKind == "company" || room.Order_id.HasValue)
            return null;

        int? peerUserId = null;
        if (roomKind == "direct" || room.Participants.Count(p => p.Is_active) == 2)
        {
            peerUserId = room.Participants
                .FirstOrDefault(p => p.Is_active && p.User_id != userId)?.User_id;

            if (!peerUserId.HasValue)
            {
                peerUserId = roomMsgs
                    .Where(m => m.Sender_id != userId)
                    .Select(m => (int?)m.Sender_id)
                    .FirstOrDefault();
            }

            if (!peerUserId.HasValue && TryParseLegacyDirectName(room.Name, userId, out var parsedPeerId))
                peerUserId = parsedPeerId;
        }

        return peerUserId;
    }

    private static string BuildRoomListDedupKey(string roomKind, ChatRoom room, int? peerUserId)
    {
        if (peerUserId is > 0 && room.Order_id == null && roomKind != "company")
            return $"direct:{peerUserId.Value}";
        if (roomKind == "company")
            return "company";
        if (roomKind == "order" && room.Order_id is > 0)
            return $"order:{room.Order_id.Value}";
        return $"room:{room.ID_ChatRoom}";
    }

    /// <summary>Оставляет одну активную личную комнату на пару пользователей (остальные скрывает из списка).</summary>
    private async Task ConsolidateDuplicateDirectRoomsAsync(int userId)
    {
        var participations = await _context.ChatParticipants
            .Include(p => p.ChatRoom)
                .ThenInclude(r => r.ChatRoomType)
            .Include(p => p.ChatRoom)
                .ThenInclude(r => r.Participants)
            .Where(p => p.User_id == userId && p.Is_active)
            .ToListAsync();

        var byPeer = new Dictionary<int, List<(int RoomId, DateTime SortAt)>>();
        foreach (var part in participations)
        {
            var room = part.ChatRoom;
            if (room.Order_id.HasValue || NormalizeRoomKind(room.ChatRoomType?.Name) == "company")
                continue;

            var peerIds = room.Participants
                .Where(p => p.Is_active && p.User_id != userId)
                .Select(p => p.User_id)
                .Distinct()
                .ToList();
            if (peerIds.Count != 1)
                continue;

            var peerId = peerIds[0];
            if (!IsDirectRoomCandidate(room, userId, peerId))
                continue;

            var sortAt = room.LastMessage_at ?? room.Created_at;
            if (!byPeer.TryGetValue(peerId, out var bucket))
                bucket = new List<(int, DateTime)>();
            bucket.Add((room.ID_ChatRoom, sortAt));
            byPeer[peerId] = bucket;
        }

        var changed = false;
        foreach (var bucket in byPeer.Values)
        {
            if (bucket.Count <= 1)
                continue;

            var canonicalId = bucket.OrderByDescending(x => x.SortAt).ThenByDescending(x => x.RoomId).First().RoomId;
            foreach (var roomId in bucket.Select(x => x.RoomId).Where(id => id != canonicalId))
            {
                var stale = participations.First(p => p.ChatRoom_id == roomId);
                stale.Is_active = false;
                stale.Left_at = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
            await _context.SaveChangesAsync();
    }

    private sealed class RoomListBuildRow
    {
        public int ChatRoomId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string RoomKind { get; init; } = string.Empty;
        public int? PeerUserId { get; init; }
        public string? LastMessageText { get; init; }
        public DateTime LastMessageAt { get; init; }
        public int UnreadCount { get; set; }
        public string DedupKey { get; init; } = string.Empty;
    }

    private static bool TryParseLegacyDirectName(string? roomName, int currentUserId, out int peerUserId)
    {
        peerUserId = 0;
        if (string.IsNullOrWhiteSpace(roomName))
            return false;

        var s = roomName.Trim();
        if (!s.StartsWith("ЛС", StringComparison.OrdinalIgnoreCase))
            return false;

        var colonIndex = s.IndexOf(':');
        if (colonIndex < 0 || colonIndex >= s.Length - 1)
            return false;

        var pair = s[(colonIndex + 1)..].Trim();
        var parts = pair.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var id1) || !int.TryParse(parts[1], out var id2))
            return false;

        peerUserId = id1 == currentUserId ? id2 : id1;
        if (peerUserId <= 0)
            peerUserId = id2 == currentUserId ? id1 : id2;
        return peerUserId > 0;
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
        {
            // Clients.User — надёжная доставка по IUserIdProvider (JWT sub / NameIdentifier).
            await _hubContext.Clients.User(userId.ToString()).SendAsync(method, payload);
            // Дублируем в кастомную группу для старых клиентов.
            await _hubContext.Clients.Group($"User_{userId}").SendAsync(method, payload);
        }
    }

    // #region agent log — REMOVED (debug code)
    // #endregion
}
