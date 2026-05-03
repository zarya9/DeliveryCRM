using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateClientProfileRequest
    {
        [MaxLength(100)]
        public string? FName { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Patronumic { get; set; }

        [MaxLength(500)]
        public string? Default_address { get; set; }

        public int? Preferred_payment_method_id { get; set; }
    }
}

