namespace WebBlazorDeliveryCRM.Models;

public sealed class ChatIncomingMessage
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public int SenderId { get; set; }
    public string? SenderName { get; set; }
    public string MessageText { get; set; } = "";
    public string? AttachmentUrl { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}
