using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class ChatRoomType
    {
        [Key]
        public int ID_ChatRoomType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    }
}

