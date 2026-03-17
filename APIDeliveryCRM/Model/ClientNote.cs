using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ClientNote
    {
        [Key]
        public int ID_ClientNote { get; set; }

        [Required]
        [ForeignKey(nameof(ClientProfile))]
        public int ClientProfile_id { get; set; }
        public ClientProfile ClientProfile { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Author))]
        public int Author_id { get; set; }
        public User Author { get; set; } = null!;

        [ForeignKey(nameof(ClientNoteType))]
        public int ClientNoteType_id { get; set; }
        public ClientNoteType ClientNoteType { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = string.Empty;

        public DateTime Created_at { get; set; } = DateTime.UtcNow;
    }
}

