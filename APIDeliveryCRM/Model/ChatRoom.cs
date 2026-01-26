using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ChatRoom
    {
        [Key]
        public int ID_ChatRoom { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [MaxLength(200)]
        public string? Name { get; set; }

        [Required]
        [ForeignKey(nameof(ChatRoomType))]
        public int ChatRoomType_id { get; set; }
        public ChatRoomType ChatRoomType { get; set; } = null!;

        [ForeignKey(nameof(Order))]
        public int? Order_id { get; set; }
        public Order? Order { get; set; }

        public DateTime Created_at { get; set; }
        public DateTime? LastMessage_at { get; set; }

        public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}

