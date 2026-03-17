using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class LeadSource
    {
        [Key]
        public int ID_LeadSource { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}

