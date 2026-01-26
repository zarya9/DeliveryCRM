using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class ScheduleType
    {
        [Key]
        public int ID_SheduleType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
