namespace WebBlazorDeliveryCRM.Models;

public class VehicleDto
{
    public int ID_Vehicle { get; set; }
    public int Company_id { get; set; }
    public string? License_plate { get; set; }
    public string? VIN { get; set; }
    public int Category_id { get; set; }
    public int BodyType_id { get; set; }
    public int FuelType_id { get; set; }
    public DateOnly Year { get; set; }
    public string? Color { get; set; }
    public decimal Cargo_volume { get; set; }
    public decimal Max_cargo_weight { get; set; }
    public decimal FuelTank_Capacity { get; set; }
    public decimal Current_mileage { get; set; }
    public string? Insurance_policy { get; set; }
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
    public VehicleBodyTypeNavDto? VehicleBodyType { get; set; }
    public FuelTypeNavDto? FuelType { get; set; }
}

public class VehicleBodyTypeNavDto
{
    public int ID_BodyType { get; set; }
    public string? Name { get; set; }
}

public class FuelTypeNavDto
{
    public int ID_FuelType { get; set; }
    public string? Name { get; set; }
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
