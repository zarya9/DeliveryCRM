using System;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class RegisterLogisticianRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FName { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Patronumic { get; set; }

        public string? Phone { get; set; }
    }
}

