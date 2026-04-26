using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpsertCommunicationTemplateRequest
    {
        [Required]
        [MaxLength(80)]
        public string Code { get; set; } = string.Empty;

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
