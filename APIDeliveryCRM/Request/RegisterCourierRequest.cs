using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class RegisterCourierRequest
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
        public string? DriverLicense { get; set; }
        public string? Passport_series { get; set; }
        public string? Passport_number { get; set; }
        public string? Passport_issued_by { get; set; }
        public DateTime? Passport_issued_date { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        
        [Required]
        public int VehicleCategory_id { get; set; }
        
        [Required]
        public int WorkSchedule_id { get; set; }
    }
}
