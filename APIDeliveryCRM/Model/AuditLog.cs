using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class AuditLog
    {
        [Key]
        public int ID_AuditLog { get; set; }

        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;

        [Required]
        public int RecordId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE

        [MaxLength(500)]
        public string? FieldName { get; set; }

        [MaxLength(1000)]
        public string? OldValue { get; set; }

        [MaxLength(1000)]
        public string? NewValue { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [ForeignKey(nameof(User))]
        public int? User_id { get; set; }
        public User? User { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        public DateTime Created_at { get; set; }
    }
}

