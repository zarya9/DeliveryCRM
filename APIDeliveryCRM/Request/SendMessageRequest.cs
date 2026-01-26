using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class SendMessageRequest
    {
        [Required]
        public string MessageText { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }
    }
}

