using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Report
    {
        [Key]
        public int ID_Report { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty; 

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(ReportStatus))]
        public int Status_id { get; set; }
        public ReportStatus ReportStatus { get; set; } = null!;

        public DateTime Created_at { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
    }
}

