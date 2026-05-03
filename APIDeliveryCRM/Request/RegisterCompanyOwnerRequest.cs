using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class RegisterCompanyOwnerRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FName { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Patronumic { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string SubscriptionPlan { get; set; } = "Pro";

        [Range(1, 100000)]
        public int MaxUsers { get; set; } = 100;

        [Range(1, 1000000)]
        public int MaxOrdersPerMonth { get; set; } = 10000;

        [Range(1, 72)]
        public int SlaOnTimeHours { get; set; } = 4;

        [Range(1, 168)]
        public int SlaLateHours { get; set; } = 24;
    }
}
