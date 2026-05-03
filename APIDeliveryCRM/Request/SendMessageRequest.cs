using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class SendMessageRequest
    {
    [MaxLength(4000)]
    public string? MessageText { get; set; }

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }
    }
}

