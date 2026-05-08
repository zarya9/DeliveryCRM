using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace APIDeliveryCRM.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ContextDB _context;
        private readonly IPasswordHasher<Login> _passwordHasher;

        public EmployeeService(ContextDB context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Login>();
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId)
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Company_id == companyId && u.Is_Active)
                .OrderBy(u => u.FName)
                .ThenBy(u => u.Name)
                .Select(u => new
                {
                    id = u.ID_User,
                    fullName = $"{u.FName} {u.Name} {u.Patronumic}".Trim(),
                    u.Patronumic,
                    role = u.Role.Name,
                    u.Is_Active,
                    isFired = !u.Is_Active,
                    u.Created_at,
                    u.Company_id,
                    email = u.Logins
                        .OrderBy(l => l.ID_Login)
                        .Select(l => l.Email)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new OkObjectResult(users);
        }

        public async Task<IActionResult> CreateAsync(CreateEmployeeRequest request, int companyId)
        {
            if (!await _context.Roles.AnyAsync(r => r.ID_Role == request.RoleId))
            {
                return new BadRequestObjectResult(new { message = "Указанная роль не найдена" });
            }

            var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == request.Email);
            if (existingLogin != null)
            {
                return new BadRequestObjectResult(new { message = "Пользователь с таким email уже существует" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles.FirstAsync(r => r.ID_Role == request.RoleId);
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == companyId);
                if (company == null)
                {
                    return new BadRequestObjectResult(new { message = "Компания не найдена" });
                }

                var user = new User
                {
                    FName = request.FName,
                    Name = request.Name,
                    Patronumic = request.Patronymic,
                    Created_at = DateTime.UtcNow,
                    Is_Active = true,
                    Theme = "light",
                    Avatar = "/avatars/default.png",
                    Company_id = company.ID_Company,
                    Company = company,
                    Role_id = role.ID_Role,
                    Role = role
                };

                var login = new Login
                {
                    Email = request.Email,
                    Password = string.Empty,
                    User = user
                };
                login.Password = _passwordHasher.HashPassword(login, request.Password);

                _context.Logins.Add(login);
                await _context.SaveChangesAsync();

                if (string.Equals(role.Name, "Курьер", StringComparison.OrdinalIgnoreCase))
                    await CreateCourierProfileInTransactionAsync(user.ID_User, company.ID_Company);

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,
                    userId = user.ID_User
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { message = $"Ошибка при создании сотрудника: {ex.Message}" });
            }
        }

        public async Task<IActionResult> FireAsync(int employeeId, int companyId, int actorUserId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ID_User == employeeId && u.Company_id == companyId);
            if (user == null)
                return new NotFoundObjectResult(new { message = "Сотрудник не найден." });

            if (user.ID_User == actorUserId)
                return new BadRequestObjectResult(new { message = "Нельзя уволить текущего пользователя." });

            if (!user.Is_Active)
                return new OkObjectResult(new { message = "Сотрудник уже уволен." });

            user.Is_Active = false;
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Сотрудник уволен." });
        }

        public async Task<IActionResult> ChangeRoleAsync(int employeeId, int companyId, int actorUserId, int roleId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ID_User == employeeId && u.Company_id == companyId && u.Is_Active);
            if (user == null)
                return new NotFoundObjectResult(new { message = "Сотрудник не найден." });
            if (user.ID_User == actorUserId)
                return new BadRequestObjectResult(new { message = "Нельзя менять роль самому себе." });

            var targetRole = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.ID_Role == roleId);
            if (targetRole == null)
                return new BadRequestObjectResult(new { message = "Роль не найдена." });

            var oldRoleName = user.Role?.Name ?? string.Empty;
            var newRoleName = targetRole.Name ?? string.Empty;

            AppendDebugLog("pre-fix", "H3", "EmployeeService.cs:ChangeRoleAsync:156", "Loaded role names", new { employeeId, companyId, actorUserId, roleId, oldRoleName, newRoleName });

            var oldRole = oldRoleName.Trim().ToLowerInvariant();
            var newRole = newRoleName.Trim().ToLowerInvariant();
            var isOldLogistic = oldRole.Contains("\u043b\u043e\u0433\u0438\u0441\u0442");
            var isOldCourier = oldRole.Contains("\u043a\u0443\u0440\u044c\u0435\u0440");
            var isNewLogistic = newRole.Contains("\u043b\u043e\u0433\u0438\u0441\u0442");
            var isNewCourier = newRole.Contains("\u043a\u0443\u0440\u044c\u0435\u0440");
            var canChange =
                (isOldLogistic && isNewCourier) ||
                (isOldCourier && isNewLogistic);

            AppendDebugLog("post-fix", "H3", "EmployeeService.cs:ChangeRoleAsync:168", "Computed role transition flag", new { oldRoleName, newRoleName, oldRole, newRole, isOldLogistic, isOldCourier, isNewLogistic, isNewCourier, canChange });

            if (!canChange)
                return new BadRequestObjectResult(new { message = "\u0420\u0430\u0437\u0440\u0435\u0448\u0435\u043d\u043e \u043c\u0435\u043d\u044f\u0442\u044c \u0442\u043e\u043b\u044c\u043a\u043e \u041b\u043e\u0433\u0438\u0441\u0442 \u2194 \u041a\u0443\u0440\u044c\u0435\u0440." });

            user.Role_id = targetRole.ID_Role;
            await _context.SaveChangesAsync();

            AppendDebugLog("pre-fix", "H4", "EmployeeService.cs:ChangeRoleAsync:170", "Role update saved", new { employeeId, newRoleId = targetRole.ID_Role, newRoleName });

            if (string.Equals(newRoleName, "Курьер", StringComparison.OrdinalIgnoreCase))
                await CreateCourierProfileInTransactionAsync(user.ID_User, companyId);

            return new OkObjectResult(new { message = $"Роль изменена: {oldRoleName} → {newRoleName}." });
        }

        private async Task CreateCourierProfileInTransactionAsync(int userId, int companyId)
        {
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
                LastActivity_at = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private static void AppendDebugLog(string runId, string hypothesisId, string location, string message, object data)
        {
            try
            {
                var line = JsonSerializer.Serialize(new
                {
                    sessionId = "7b40bb",
                    runId,
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                File.AppendAllText(@"c:\Users\zarip\source\repos\DeliveryCRM\debug-7b40bb.log", line + Environment.NewLine);
            }
            catch { }
        }
    }
}

