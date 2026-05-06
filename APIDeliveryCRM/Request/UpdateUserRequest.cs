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

        [MaxLength(256)]
        [EmailAddress]
        public string? NewEmail { get; set; }

        [MaxLength(128)]
        public string? NewPassword { get; set; }

        [MaxLength(128)]
        public string? CurrentPassword { get; set; }
    }
}

