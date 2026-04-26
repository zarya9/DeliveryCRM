using System;

namespace APIDeliveryCRM.Request
{
    /// <summary>Создание ТС логистом (поля соответствуют таблице Vehicles).</summary>
    public class CreateVehicleRequest
    {
        public string License_plate { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public int Category_id { get; set; }

        /// <summary>Опционально: привязка к справочнику VehicleModels (админ/интеграции).</summary>
        public int? Model_id { get; set; }

        /// <summary>Марка и модель вручную (основной сценарий логиста).</summary>
        public string Brand_name { get; set; } = string.Empty;
        public string Model_name { get; set; } = string.Empty;

        public DateOnly Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public int BodyType_id { get; set; }
        public decimal Cargo_volume { get; set; }
        public decimal Max_cargo_weight { get; set; }
        public int FuelType_id { get; set; }
        public decimal FuelTank_Capacity { get; set; }
        public decimal Current_mileage { get; set; }
        public string Insurance_policy { get; set; } = string.Empty;
        public DateTime? Insurance_expires_at { get; set; }
        public DateTime? Registration_expires_at { get; set; }
        public DateTime? Maintenance_due_at { get; set; }
        public bool Is_available { get; set; } = true;

        /// <summary>Сразу закрепить за курьером; null — только автопарк.</summary>
        public int? CurrentCourier_id { get; set; }
    }
}
