using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class CommunicationTemplate
    {
        [Key]
        public int ID_CommunicationTemplate { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string Code { get; set; } = string.Empty; // ORDER_STATUS_CHANGED, ORDER_DELAYED

        [Required]
        [MaxLength(200)]
        public string TitleTemplate { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string BodyTemplate { get; set; } = string.Empty;

        public int? TriggerStatus_id { get; set; }
        public bool Is_active { get; set; } = true;
    }
}
