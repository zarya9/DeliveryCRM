using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? FName { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Patronumic { get; set; }
    }
}

