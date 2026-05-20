using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Notification
    {
        [Key]
        public int ID_Notification { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; }

        [Required]
        [ForeignKey(nameof(NotificationType))]
        public int Type_id { get; set; }
        public NotificationType NotificationType { get; set; } = null!;

        public string Title { get; set; }
        public string Message { get; set; }

        [ForeignKey(nameof(Order))]
        public int? Order_id { get; set; }
        public Order? Order { get; set; }

        [ForeignKey(nameof(CourierShift))]
        public int? Shift_id { get; set; }
        public CourierShift? CourierShift { get; set; }

        public bool Is_read { get; set; }
        public byte Priority { get; set; } = 0;
        public bool Is_critical { get; set; }
        public bool Requires_ack { get; set; }
        public DateTime? Acknowledged_at { get; set; }
        public DateOnly Sent_at { get; set; }
    }
}
