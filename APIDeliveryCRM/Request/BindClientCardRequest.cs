using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class BindClientCardRequest
    {
        [Required]
        [MaxLength(64)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(5)]
        public string Expiry { get; set; } = string.Empty; 

        [Required]
        [MaxLength(120)]
        public string CardHolder { get; set; } = string.Empty;

        [Required]
        [MaxLength(4)]
        public string Cvv { get; set; } = string.Empty;
    }
}
