using System;

namespace APIDeliveryCRM.Responses
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Subdomain { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public bool IsActive { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public int MaxOrdersPerMonth { get; set; }
        public DateTime SubscriptionExpiresAt { get; set; }
        public int SlaOnTimeHours { get; set; }
        public int SlaLateHours { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
