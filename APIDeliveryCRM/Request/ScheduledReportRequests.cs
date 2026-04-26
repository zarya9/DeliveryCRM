using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpsertScheduledReportJobRequest
    {
        public int? JobId { get; set; }

        [Required]
        public string ReportType { get; set; } = "FINANCE";

        [Required]
        public string Frequency { get; set; } = "Daily";

        [Required]
        [RegularExpression(@"^\d{2}:\d{2}$")]
        public string TimeUtc { get; set; } = "06:00";

        public int? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public bool Is_active { get; set; } = true;
    }
}
