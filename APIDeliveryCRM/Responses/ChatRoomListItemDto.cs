namespace APIDeliveryCRM.Responses;

public class ChatRoomListItemDto
{
    public int ChatRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomKind { get; set; } = "company";
    public int? PeerUserId { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }

    /// <summary>Число непрочитанных входящих сообщений для текущего пользователя.</summary>
    public int UnreadCount { get; set; }
}
