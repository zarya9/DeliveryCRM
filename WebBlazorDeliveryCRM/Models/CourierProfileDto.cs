namespace WebBlazorDeliveryCRM.Models;

public class CourierProfileDto
{
    public int ID_CourierProfile { get; set; }
    public int Company_id { get; set; }
    public int User_id { get; set; }
    public decimal Rating { get; set; }
    public int Total_deliveries { get; set; }
    public bool Is_online { get; set; }
    public decimal Current_lat { get; set; }
    public decimal Current_lon { get; set; }
    public DateTime LastActivity_at { get; set; }
    public string? DriverLicense { get; set; }
    public string? Passport_data { get; set; }
    public UserDto? User { get; set; }
    public CourierStatusDto? CourierStatus { get; set; }
    public string? VehicleCategoryName { get; set; }
    public VehicleCategoryNavDto? VehicleCategory { get; set; }
    public string? ScheduleName { get; set; }
    public ScheduleTypeNavDto? ScheduleType { get; set; }
}

public class VehicleCategoryNavDto
{
    public int ID_Category { get; set; }
    public string Name { get; set; } = "";
}

public class ScheduleTypeNavDto
{
    public int ID_SheduleType { get; set; }
    public string? Name { get; set; }
}

public class CourierStatusDto
{
    public int ID_CourierStatus { get; set; }
    public string Name { get; set; } = "";
}
