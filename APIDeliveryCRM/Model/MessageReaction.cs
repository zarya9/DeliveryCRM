using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class MessageReaction
    {
        [Key]
        public int ID_MessageReaction { get; set; }

        [Required]
        [ForeignKey(nameof(ChatMessage))]
        public int ChatMessage_id { get; set; }
        public ChatMessage ChatMessage { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        /// <summary>Emoji символ или код, например "👍" или ":thumbsup:"</summary>
        [Required]
        [MaxLength(50)]
        public string Emoji { get; set; } = string.Empty;

        public DateTime Created_at { get; set; } = DateTime.UtcNow;
    }
}
