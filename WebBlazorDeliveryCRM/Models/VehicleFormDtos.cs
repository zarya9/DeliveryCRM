namespace WebBlazorDeliveryCRM.Models;

public class VehicleFormLookupsDto
{
    public List<IdNameDto> Categories { get; set; } = new();
    public List<IdNameDto> BodyTypes { get; set; } = new();
    public List<IdNameDto> FuelTypes { get; set; } = new();
}

public class IdNameDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>Модель из справочника VehicleModels (ответ GET catalog/models).</summary>
public class VehicleCatalogModelDto
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? Name { get; set; }
    public DateOnly Year { get; set; }
    public decimal AvgFuelCity { get; set; }
    public decimal AvgFuelHighWay { get; set; }
    public decimal EngineCapacity { get; set; }
    public int HorsePower { get; set; }
    public int TransmissionTypeId { get; set; }
    public int DriveTypeId { get; set; }
    public string? TransmissionTypeName { get; set; }
    public string? DriveTypeName { get; set; }
}

/// <summary>Тело POST /api/Vehicles — совпадает с API.</summary>
public class CreateVehicleApiRequest
{
    public string License_plate { get; set; } = "";
    public string VIN { get; set; } = "";
    public int Category_id { get; set; }
    public int? Model_id { get; set; }
    public string Brand_name { get; set; } = "";
    public string Model_name { get; set; } = "";
    public DateOnly Year { get; set; }
    public string Color { get; set; } = "";
    public int BodyType_id { get; set; }
    public decimal Cargo_volume { get; set; }
    public decimal Max_cargo_weight { get; set; }
    public int FuelType_id { get; set; }
    public decimal FuelTank_Capacity { get; set; }
    public decimal Current_mileage { get; set; }
    public string Insurance_policy { get; set; } = "";
    public DateTime? Insurance_expires_at { get; set; }
    public DateTime? Registration_expires_at { get; set; }
    public DateTime? Maintenance_due_at { get; set; }
    public bool Is_available { get; set; } = true;
    public int? CurrentCourier_id { get; set; }
}

public class AuditLogRowDto
{
    public int ID_AuditLog { get; set; }
    public string? TableName { get; set; }
    public int RecordId { get; set; }
    public string? Action { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public string? UserName { get; set; }
    public int? User_id { get; set; }
    public DateTime Created_at { get; set; }
}
