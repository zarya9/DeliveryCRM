using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class NotificationType
    {
        [Key]
        public int ID_NotificationType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
