using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ScheduledReportJob
    {
        [Key]
        public int ID_ScheduledReportJob { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = "FINANCE";

        [Required]
        [MaxLength(20)]
        public string Frequency { get; set; } = "Daily"; // Daily, Weekly, Monthly

        /// <summary>Время запуска в UTC: HH:mm.</summary>
        [Required]
        [MaxLength(5)]
        public string TimeUtc { get; set; } = "06:00";

        public int? DayOfWeek { get; set; } // 0..6 if weekly
        public int? DayOfMonth { get; set; } // 1..28/31 if monthly

        public bool Is_active { get; set; } = true;
        public DateTime? LastRun_at { get; set; }
        public DateTime NextRun_at { get; set; }
    }
}
