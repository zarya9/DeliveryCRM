using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ChatParticipant
    {
        [Key]
        public int ID_ChatParticipant { get; set; }

        [Required]
        [ForeignKey(nameof(ChatRoom))]
        public int ChatRoom_id { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        public DateTime Joined_at { get; set; }
        public DateTime? Left_at { get; set; }
        public bool Is_active { get; set; }
        public DateTime? LastRead_at { get; set; }
    }
}

