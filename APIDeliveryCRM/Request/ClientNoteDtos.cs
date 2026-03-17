using System;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class AddClientNoteRequest
    {
        [Required]
        public int ClientProfileId { get; set; }

        [Required]
        public int AuthorUserId { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = "NOTE";

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = string.Empty;
    }

    public class ClientNoteShortDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}

