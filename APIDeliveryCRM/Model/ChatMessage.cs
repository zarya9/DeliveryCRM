using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ChatMessage
    {
        [Key]
        public int ID_ChatMessage { get; set; }

        [Required]
        [ForeignKey(nameof(ChatRoom))]
        public int ChatRoom_id { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Sender))]
        public int Sender_id { get; set; }
        public User Sender { get; set; } = null!;

        [Required]
        [MaxLength(5000)]
        public string MessageText { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        public DateTime Sent_at { get; set; }
        public DateTime? Edited_at { get; set; }
        public bool Is_deleted { get; set; }
    }
}

