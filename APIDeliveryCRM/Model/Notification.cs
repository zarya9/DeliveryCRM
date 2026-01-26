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

        [Required]
        [ForeignKey(nameof(Order))]
        public int? Order_id { get; set; }
        public Order Order { get; set; }

        public bool Is_read { get; set; }
        public DateOnly Sent_at { get; set; }
    }
}
