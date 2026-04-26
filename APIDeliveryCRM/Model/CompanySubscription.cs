using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class CompanySubscription
    {
        [Key]
        public int ID_CompanySubscription { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Plan))]
        public int SubscriptionPlan_id { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "trialing";

        public DateTime Started_at { get; set; } = DateTime.UtcNow;
        public DateTime CurrentPeriodStart_at { get; set; } = DateTime.UtcNow;
        public DateTime CurrentPeriodEnd_at { get; set; }
        public DateTime? Canceled_at { get; set; }
        public bool AutoRenew { get; set; } = true;
    }
}
