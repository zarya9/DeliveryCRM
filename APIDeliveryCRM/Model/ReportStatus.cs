using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class ReportStatus
    {
        [Key]
        public int ID_ReportStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}

