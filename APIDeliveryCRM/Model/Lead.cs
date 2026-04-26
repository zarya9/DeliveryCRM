using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Lead
    {
        [Key]
        public int ID_Lead { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Contact { get; set; }

        [ForeignKey(nameof(Source))]
        public int LeadSource_id { get; set; }
        public LeadSource Source { get; set; } = null!;

        [ForeignKey(nameof(Stage))]
        public int LeadStage_id { get; set; }
        public LeadStage Stage { get; set; } = null!;

        [ForeignKey(nameof(Manager))]
        public int? ManagerUser_id { get; set; }
        public User? Manager { get; set; }

        public DateTime Created_at { get; set; } = DateTime.UtcNow;
        public DateTime? Updated_at { get; set; }
        public DateTime? Won_at { get; set; }
        public DateTime? Lost_at { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [MaxLength(500)]
        public string? Lost_reason { get; set; }

        [MaxLength(200)]
        public string? NextTask_title { get; set; }
        public DateTime? NextTask_due_at { get; set; }
    }
}

