using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class CreateSupportTicketRequest
    {
        public int? Order_id { get; set; }
        public int? ClientProfile_id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 4)]
        public byte Category { get; set; } = 4;

        [Range(0, 2)]
        public byte Priority { get; set; } = 0;

        public int? ResponsibleUser_id { get; set; }
    }
}
