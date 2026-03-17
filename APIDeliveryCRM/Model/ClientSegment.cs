using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class ClientSegment
    {
        [Key]
        public int ID_ClientSegment { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public ICollection<ClientProfile> ClientProfiles { get; set; } = new List<ClientProfile>();
    }
}

