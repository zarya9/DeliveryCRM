using System;

namespace APIDeliveryCRM.Responses
{
    public class NotificationItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? TypeName { get; set; }
        public int? OrderId { get; set; }
        public bool IsRead { get; set; }
        public byte Priority { get; set; }
        public bool IsCritical { get; set; }
        public bool RequiresAck { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateOnly SentAt { get; set; }
    }
}
