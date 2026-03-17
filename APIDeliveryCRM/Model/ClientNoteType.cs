using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class ClientNoteType
    {
        [Key]
        public int ID_ClientNoteType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public ICollection<ClientNote> ClientNotes { get; set; } = new List<ClientNote>();
    }
}

