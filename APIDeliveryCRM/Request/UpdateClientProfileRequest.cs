using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateClientProfileRequest
    {
        [MaxLength(500)]
        public string? Default_address { get; set; }

        public int? Preferred_payment_method_id { get; set; }
    }
}

