using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Helpers;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace APIDeliveryCRM.Services
{
    public class CourierService : ICourierService
    {
        private readonly ContextDB _context;
        private readonly IAuditService _audit;
        private readonly IShiftService _shifts;

        public CourierService(ContextDB context, IAuditService audit, IShiftService shifts)
        {
            _context = context;
            _audit = audit;
            _shifts = shifts;
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
            if (companyId.HasValue)
                await SyncMissingCourierProfilesForCompanyAsync(companyId.Value);

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

        private async Task SyncMissingCourierProfilesForCompanyAsync(int companyId)
        {
            var courierRoleId = await _context.Roles.AsNoTracking()
                .Where(r => r.Name == "Курьер")
                .Select(r => r.ID_Role)
                .FirstOrDefaultAsync();
            if (courierRoleId == 0)
                return;

            var courierUserIds = await _context.Users.AsNoTracking()
                .Where(u => u.Company_id == companyId && u.Is_Active && u.Role_id == courierRoleId)
                .Select(u => u.ID_User)
                .ToListAsync();

            var usersWithProfile = await _context.CourierProfiles.AsNoTracking()
                .Where(cp => cp.Company_id == companyId)
                .Select(cp => cp.User_id)
                .ToListAsync();

            var missingUserIds = courierUserIds.Where(id => !usersWithProfile.Contains(id)).ToList();
            if (missingUserIds.Count == 0)
                return;

            var defaultCategory = await _context.VehicleCategories.OrderBy(c => c.ID_Category).FirstOrDefaultAsync();
            var defaultSchedule = await _context.ScheduleTypes.OrderBy(s => s.ID_SheduleType).FirstOrDefaultAsync();
            if (defaultCategory == null || defaultSchedule == null)
                return;

            var defaultStatus = await _context.CourierStatuses.FirstOrDefaultAsync(s => s.Name == "Не на смене");
            if (defaultStatus == null)
            {
                defaultStatus = new CourierStatus { Name = "Не на смене", Description = "Курьер не на смене" };
                _context.CourierStatuses.Add(defaultStatus);
                await _context.SaveChangesAsync();
            }

            foreach (var uid in missingUserIds)
            {
                var user = await _context.Users.FirstAsync(u => u.ID_User == uid);
                var profile = new CourierProfile
                {
                    Company_id = companyId,
                    User_id = uid,
                    VehicleCategory_id = defaultCategory.ID_Category,
                    WorkSchedule_id = defaultSchedule.ID_SheduleType,
                    CurrentStatus_id = defaultStatus.ID_CourierStatus,
                    Total_deliveries = 0,
                    Current_lat = 0,
                    Current_lon = 0,
                    Rating = 0,
                    Is_online = false,
                    LastActivity_at = System.DateTime.UtcNow
                };
                _context.CourierProfiles.Add(profile);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Order>> GetActiveOrdersAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId && o.Delivered_at == null)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.RouteStops)
                    .ThenInclude(s => s.Address)
                .Include(o => o.RouteStops)
                    .ThenInclude(s => s.LogisticsHub)
                        .ThenInclude(h => h.Address)
                .Include(o => o.OriginHub)
                    .ThenInclude(h => h!.Address)
                .Include(o => o.DestinationHub)
                    .ThenInclude(h => h!.Address)
                .ToListAsync();
        }

        public async Task EnsureCourierProfileForUserAsync(int userId, int companyId)
        {
            var courierRoleId = await _context.Roles.AsNoTracking()
                .Where(r => r.Name == "Курьер")
                .Select(r => r.ID_Role)
                .FirstOrDefaultAsync();
            if (courierRoleId == 0)
                return;

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID_User == userId && u.Company_id == companyId && u.Role_id == courierRoleId);
            if (user == null)
                return;

            if (await _context.CourierProfiles.AnyAsync(c => c.User_id == userId))
                return;

            var defaultCategory = await _context.VehicleCategories.OrderBy(c => c.ID_Category).FirstOrDefaultAsync();
            var defaultSchedule = await _context.ScheduleTypes.OrderBy(s => s.ID_SheduleType).FirstOrDefaultAsync();
            if (defaultCategory == null || defaultSchedule == null)
                return;

            var defaultStatus = await _context.CourierStatuses.FirstOrDefaultAsync(s => s.Name == "Не на смене");
            if (defaultStatus == null)
            {
                defaultStatus = new CourierStatus { Name = "Не на смене", Description = "Курьер не на смене" };
                _context.CourierStatuses.Add(defaultStatus);
                await _context.SaveChangesAsync();
            }

            _context.CourierProfiles.Add(new CourierProfile
            {
                Company_id = companyId,
                User_id = userId,
                VehicleCategory_id = defaultCategory.ID_Category,
                WorkSchedule_id = defaultSchedule.ID_SheduleType,
                CurrentStatus_id = defaultStatus.ID_CourierStatus,
                Total_deliveries = 0,
                Current_lat = 0,
                Current_lon = 0,
                Rating = 0,
                Is_online = false,
                LastActivity_at = System.DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLocationAsync(int courierProfileId, decimal lat, decimal lon)
        {
            var active = await _shifts.GetActiveShiftAsync(courierProfileId);
            if (active == null)
                throw new InvalidOperationException("Передача координат доступна только во время активной смены.");

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

            if (isOnline)
            {
                var active = await _shifts.GetActiveShiftAsync(courierProfileId);
                if (active == null)
                    throw new InvalidOperationException("Выйти «в сеть» можно только после начала смены.");
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
            if (courier.VehicleCategory_id != vehicle.Category_id)
                throw new InvalidOperationException("Категория ТС не соответствует допуску курьера.");

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
            dto.License_plate = NormalizeLicensePlate(dto.License_plate);
            if (!IsValidLicensePlate(dto.License_plate))
                throw new InvalidOperationException("Гос. номер должен быть в формате А123ВС77 (допустимы только буквы АВЕКМНОРСТУХ и цифры).");
            if (!VinHelper.IsValid(dto.VIN, out var vinError))
                throw new InvalidOperationException(vinError ?? "Некорректный VIN.");

            var category = await _context.VehicleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.ID_Category == dto.Category_id);
            if (category == null)
                throw new InvalidOperationException("Категория ТС не найдена.");

            var brandName = (dto.Brand_name ?? string.Empty).Trim();
            var modelName = (dto.Model_name ?? string.Empty).Trim();
            var year = dto.Year;

            if (dto.Model_id is int catalogModelId && catalogModelId > 0)
            {
                var catalogModel = await _context.VehicleModels.AsNoTracking()
                    .Include(m => m.VehicleBrand)
                    .FirstOrDefaultAsync(m => m.ID_Model == catalogModelId);
                if (catalogModel == null)
                    throw new InvalidOperationException("Модель ТС из справочника не найдена.");
                brandName = (catalogModel.VehicleBrand?.Name ?? brandName).Trim();
                modelName = (catalogModel.Name ?? modelName).Trim();
                year = catalogModel.Year;
            }
            else if (string.IsNullOrWhiteSpace(brandName) || string.IsNullOrWhiteSpace(modelName))
            {
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
                if (cp.VehicleCategory_id != dto.Category_id)
                    throw new InvalidOperationException("Нельзя закрепить ТС: категория не соответствует допуску курьера.");
            }

            var entity = new Vehicle
            {
                Company_id = companyId,
                License_plate = dto.License_plate.Trim(),
                VIN = dto.VIN.Trim().ToUpperInvariant(),
                Category_id = dto.Category_id,
                Model_id = dto.Model_id is int mid && mid > 0 ? mid : null,
                Brand_name = brandName,
                Model_name = modelName,
                Year = year,
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

        private static string NormalizeLicensePlate(string input)
        {
            var up = (input ?? string.Empty).Trim().ToUpperInvariant();
            up = up.Replace('A', '\u0410').Replace('B', '\u0412').Replace('E', '\u0415').Replace('K', '\u041A').Replace('M', '\u041C')
                   .Replace('H', '\u041D').Replace('O', '\u041E').Replace('P', '\u0420').Replace('C', '\u0421').Replace('T', '\u0422')
                   .Replace('Y', '\u0423').Replace('X', '\u0425');
            return Regex.Replace(up, "\\s+", string.Empty);
        }

        private static bool IsValidLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return false;
            return Regex.IsMatch(plate, "^[АВЕКМНОРСТУХ]\\d{3}[АВЕКМНОРСТУХ]{2}\\d{2,3}$");
        }

        public async Task UpdateCourierDocumentsAsync(int courierProfileId, int companyId, string? driverLicense, string? passportData)
        {
            var courier = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
                throw new KeyNotFoundException("Курьер не найден.");
            if (courier.Company_id != companyId)
                throw new InvalidOperationException("Нет доступа к этому профилю курьера.");

            courier.DriverLicense = string.IsNullOrWhiteSpace(driverLicense) ? null : driverLicense.Trim();
            courier.Passport_data = string.IsNullOrWhiteSpace(passportData) ? null : passportData.Trim();
            await _context.SaveChangesAsync();
        }
    }
}


