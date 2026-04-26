using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class CourierService : ICourierService
    {
        private readonly ContextDB _context;
        private readonly IAuditService _audit;

        public CourierService(ContextDB context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<CourierProfile> GetProfileAsync(int courierProfileId)
        {
            return await _context.CourierProfiles
                .Include(c => c.User)
                    .ThenInclude(u => u.Role)
                .Include(c => c.VehicleCategory)
                .Include(c => c.ScheduleType)
                .Include(c => c.CourierStatus)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId)
                ?? throw new KeyNotFoundException("Курьер не найден.");
        }

        public async Task<CourierProfile?> GetByUserIdAsync(int userId)
        {
            return await _context.CourierProfiles
                .Include(c => c.User)
                .Include(c => c.VehicleCategory)
                .Include(c => c.CourierStatus)
                .FirstOrDefaultAsync(c => c.User_id == userId);
        }

        public async Task<IReadOnlyList<CourierProfile>> GetAllAsync(int? companyId = null)
        {
            var query = _context.CourierProfiles
                .Include(c => c.User)
                .Include(c => c.CourierStatus)
                .Include(c => c.VehicleCategory)
                .Include(c => c.ScheduleType)
                .AsQueryable();
            if (companyId.HasValue)
                query = query.Where(c => c.Company_id == companyId.Value);
            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetActiveOrdersAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .ToListAsync();
        }

        public async Task UpdateLocationAsync(int courierProfileId, decimal lat, decimal lon)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
            {
                return;
            }

            courier.Current_lat = lat;
            courier.Current_lon = lon;
            courier.LastActivity_at = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SetOnlineStatusAsync(int courierProfileId, bool isOnline)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
            {
                return;
            }

            courier.Is_online = isOnline;
            courier.LastActivity_at = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Vehicle>> GetVehiclesByCompanyAsync(int companyId)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.Company_id == companyId)
                .Include(v => v.VehicleCategory)
                .Include(v => v.VehicleModel)
                    .ThenInclude(m => m!.VehicleBrand)
                .OrderBy(v => v.License_plate)
                .ToListAsync();
        }

        public async Task AssignVehicleAsync(int courierProfileId, int vehicleId, int? actorUserId = null, string? ipAddress = null)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
                throw new KeyNotFoundException("Курьер не найден.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.ID_Vehicle == vehicleId);
            if (vehicle == null)
                throw new KeyNotFoundException("Транспортное средство не найдено.");

            if (vehicle.Company_id != courier.Company_id)
                throw new InvalidOperationException("ТС принадлежит другой компании.");

            if (!vehicle.Is_available)
                throw new InvalidOperationException("ТС временно недоступно к назначению.");

            var now = DateTime.UtcNow;
            if (vehicle.Insurance_expires_at.HasValue && vehicle.Insurance_expires_at.Value < now)
                throw new InvalidOperationException("Нельзя назначить ТС: просрочен полис страхования.");
            if (vehicle.Registration_expires_at.HasValue && vehicle.Registration_expires_at.Value < now)
                throw new InvalidOperationException("Нельзя назначить ТС: просрочена регистрация.");

            var prevCourierId = vehicle.CurrentCourier_id;
            var others = await _context.Vehicles
                .Where(v => v.Company_id == courier.Company_id
                    && v.CurrentCourier_id == courierProfileId
                    && v.ID_Vehicle != vehicleId)
                .ToListAsync();
            foreach (var v in others)
                v.CurrentCourier_id = null;

            vehicle.CurrentCourier_id = courier.ID_CourierProfile;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                courier.Company_id,
                actorUserId,
                "Vehicles",
                vehicle.ID_Vehicle,
                "UPDATE",
                $"Назначение ТС курьеру: профиль курьера {courierProfileId}, гос. номер {vehicle.License_plate}",
                fieldName: "CurrentCourier_id",
                oldValue: prevCourierId?.ToString(),
                newValue: courierProfileId.ToString(),
                ipAddress: ipAddress);
        }

        public async Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest dto, int companyId, int? actorUserId = null, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(dto.License_plate))
                throw new InvalidOperationException("Укажите гос. номер.");
            if (string.IsNullOrWhiteSpace(dto.VIN))
                throw new InvalidOperationException("Укажите VIN.");

            var category = await _context.VehicleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.ID_Category == dto.Category_id);
            if (category == null)
                throw new InvalidOperationException("Категория ТС не найдена.");

            if (dto.Model_id is int catalogModelId && catalogModelId > 0)
            {
                var exists = await _context.VehicleModels.AsNoTracking().AnyAsync(m => m.ID_Model == catalogModelId);
                if (!exists)
                    throw new InvalidOperationException("Модель ТС из справочника не найдена.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Brand_name) || string.IsNullOrWhiteSpace(dto.Model_name))
                    throw new InvalidOperationException("Укажите марку и модель ТС (вручную) или выберите модель из справочника.");
            }

            var body = await _context.VehicleBodyTypes.AsNoTracking().FirstOrDefaultAsync(b => b.ID_BodyType == dto.BodyType_id);
            if (body == null)
                throw new InvalidOperationException("Тип кузова не найден.");

            var fuel = await _context.FuelTypes.AsNoTracking().FirstOrDefaultAsync(f => f.ID_FuelType == dto.FuelType_id);
            if (fuel == null)
                throw new InvalidOperationException("Тип топлива не найден.");

            if (dto.CurrentCourier_id.HasValue)
            {
                var cp = await _context.CourierProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ID_CourierProfile == dto.CurrentCourier_id.Value);
                if (cp == null)
                    throw new InvalidOperationException("Курьер не найден.");
                if (cp.Company_id != companyId)
                    throw new InvalidOperationException("Курьер принадлежит другой компании.");
            }

            var entity = new Vehicle
            {
                Company_id = companyId,
                License_plate = dto.License_plate.Trim(),
                VIN = dto.VIN.Trim(),
                Category_id = dto.Category_id,
                Model_id = dto.Model_id is int mid && mid > 0 ? mid : null,
                Brand_name = (dto.Brand_name ?? string.Empty).Trim(),
                Model_name = (dto.Model_name ?? string.Empty).Trim(),
                Year = dto.Year,
                Color = dto.Color ?? string.Empty,
                BodyType_id = dto.BodyType_id,
                Cargo_volume = dto.Cargo_volume,
                Max_cargo_weight = dto.Max_cargo_weight,
                FuelType_id = dto.FuelType_id,
                FuelTank_Capacity = dto.FuelTank_Capacity,
                Current_mileage = dto.Current_mileage,
                Insurance_policy = dto.Insurance_policy ?? string.Empty,
                Insurance_expires_at = dto.Insurance_expires_at?.ToUniversalTime(),
                Registration_expires_at = dto.Registration_expires_at?.ToUniversalTime(),
                Maintenance_due_at = dto.Maintenance_due_at?.ToUniversalTime(),
                Is_available = dto.Is_available,
                CurrentCourier_id = dto.CurrentCourier_id
            };

            _context.Vehicles.Add(entity);
            await _context.SaveChangesAsync();

            if (dto.CurrentCourier_id.HasValue)
            {
                var others = await _context.Vehicles
                    .Where(v => v.Company_id == companyId
                        && v.CurrentCourier_id == dto.CurrentCourier_id.Value
                        && v.ID_Vehicle != entity.ID_Vehicle)
                    .ToListAsync();
                foreach (var v in others)
                    v.CurrentCourier_id = null;
                await _context.SaveChangesAsync();
            }

            await _audit.LogAsync(
                companyId,
                actorUserId,
                "Vehicles",
                entity.ID_Vehicle,
                "INSERT",
                $"Создано ТС: {entity.License_plate}, {entity.Brand_name} {entity.Model_name}, VIN {entity.VIN}",
                ipAddress: ipAddress);

            return entity;
        }

        public async Task UpdateCourierDocumentsAsync(int courierProfileId, string? driverLicense, string? passportData)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
                throw new KeyNotFoundException("Курьер не найден.");

            courier.DriverLicense = string.IsNullOrWhiteSpace(driverLicense) ? null : driverLicense.Trim();
            courier.Passport_data = string.IsNullOrWhiteSpace(passportData) ? null : passportData.Trim();
            await _context.SaveChangesAsync();
        }
    }
}


