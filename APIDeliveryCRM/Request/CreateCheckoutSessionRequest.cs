using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class CreateCheckoutSessionRequest
    {
        [Required]
        public string PlanCode { get; set; } = string.Empty;

        [Range(1, 12)]
        public int PeriodMonths { get; set; } = 1;
    }
}
