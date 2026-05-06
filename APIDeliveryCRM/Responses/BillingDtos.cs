namespace APIDeliveryCRM.Responses
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public int MaxUsers { get; set; }
        public int MaxOrdersPerMonth { get; set; }
    }

    public class CompanySubscriptionDto
    {
        public int CompanyId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime CurrentPeriodEndAt { get; set; }
        public bool AutoRenew { get; set; }
        public string? PendingPlanCode { get; set; }
        public string? PendingPlanName { get; set; }
        public DateTime? PendingPlanEffectiveAt { get; set; }
    }

    public class CheckoutSessionResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ProviderPaymentId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Action { get; set; } = "checkout";
        public string Message { get; set; } = string.Empty;
    }
}
