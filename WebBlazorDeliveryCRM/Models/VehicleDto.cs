namespace WebBlazorDeliveryCRM.Models;

public class VehicleDto
{
    public int ID_Vehicle { get; set; }
    public int Company_id { get; set; }
    public string? License_plate { get; set; }
    public string? VIN { get; set; }
    public int? CurrentCourier_id { get; set; }
    public DateTime? Insurance_expires_at { get; set; }
    public DateTime? Registration_expires_at { get; set; }
    public DateTime? Maintenance_due_at { get; set; }
    public bool Is_available { get; set; }
    public int? Model_id { get; set; }
    public string? Brand_name { get; set; }
    public string? Model_name { get; set; }
    public VehicleModelDto? VehicleModel { get; set; }
    public VehicleCategoryNavDto? VehicleCategory { get; set; }
}

public class VehicleModelDto
{
    public int ID_Model { get; set; }
    public string? Name { get; set; }
    public VehicleBrandDto? VehicleBrand { get; set; }
}

public class VehicleBrandDto
{
    public int ID_Brand { get; set; }
    public string? Name { get; set; }
}
