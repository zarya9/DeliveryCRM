using System;
using System.Linq;
using System.Threading.Tasks;
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
                return new BadRequestObjectResult(new { message = "РЈРєР°Р·Р°РЅРЅР°СЏ СЂРѕР»СЊ РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == request.Email);
            if (existingLogin != null)
            {
                return new BadRequestObjectResult(new { message = "РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ СЃ С‚Р°РєРёРј email СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles.FirstAsync(r => r.ID_Role == request.RoleId);
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == companyId);
                if (company == null)
                {
                    return new BadRequestObjectResult(new { message = "РљРѕРјРїР°РЅРёСЏ РЅРµ РЅР°Р№РґРµРЅР°" });
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

                if (string.Equals(role.Name, "РљСѓСЂСЊРµСЂ", StringComparison.Ordinal))
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
                return new BadRequestObjectResult(new { message = $"РћС€РёР±РєР° РїСЂРё СЃРѕР·РґР°РЅРёРё СЃРѕС‚СЂСѓРґРЅРёРєР°: {ex.Message}" });
            }
        }

        public async Task<IActionResult> FireAsync(int employeeId, int companyId, int actorUserId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ID_User == employeeId && u.Company_id == companyId);
            if (user == null)
                return new NotFoundObjectResult(new { message = "РЎРѕС‚СЂСѓРґРЅРёРє РЅРµ РЅР°Р№РґРµРЅ." });

            if (user.ID_User == actorUserId)
                return new BadRequestObjectResult(new { message = "РќРµР»СЊР·СЏ СѓРІРѕР»РёС‚СЊ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ." });

            if (!user.Is_Active)
                return new OkObjectResult(new { message = "РЎРѕС‚СЂСѓРґРЅРёРє СѓР¶Рµ СѓРІРѕР»РµРЅ." });

            user.Is_Active = false;
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "РЎРѕС‚СЂСѓРґРЅРёРє СѓРІРѕР»РµРЅ." });
        }

        public async Task<IActionResult> ChangeRoleAsync(int employeeId, int companyId, int actorUserId, int roleId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ID_User == employeeId && u.Company_id == companyId && u.Is_Active);
            if (user == null)
                return new NotFoundObjectResult(new { message = "РЎРѕС‚СЂСѓРґРЅРёРє РЅРµ РЅР°Р№РґРµРЅ." });
            if (user.ID_User == actorUserId)
                return new BadRequestObjectResult(new { message = "РќРµР»СЊР·СЏ РјРµРЅСЏС‚СЊ СЂРѕР»СЊ СЃР°РјРѕРјСѓ СЃРµР±Рµ." });

            var targetRole = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.ID_Role == roleId);
            if (targetRole == null)
                return new BadRequestObjectResult(new { message = "Р РѕР»СЊ РЅРµ РЅР°Р№РґРµРЅР°." });

            var oldRoleName = user.Role?.Name ?? string.Empty;
            var newRoleName = targetRole.Name ?? string.Empty;
            var canChange =
                (oldRoleName == "Р›РѕРіРёСЃС‚" && newRoleName == "РљСѓСЂСЊРµСЂ") ||
                (oldRoleName == "РљСѓСЂСЊРµСЂ" && newRoleName == "Р›РѕРіРёСЃС‚");
            if (!canChange)
                return new BadRequestObjectResult(new { message = "Р Р°Р·СЂРµС€РµРЅРѕ РјРµРЅСЏС‚СЊ С‚РѕР»СЊРєРѕ Р›РѕРіРёСЃС‚ в†” РљСѓСЂСЊРµСЂ." });

            user.Role_id = targetRole.ID_Role;
            await _context.SaveChangesAsync();

            if (newRoleName == "РљСѓСЂСЊРµСЂ")
                await CreateCourierProfileInTransactionAsync(user.ID_User, companyId);

            return new OkObjectResult(new { message = $"Р РѕР»СЊ РёР·РјРµРЅРµРЅР°: {oldRoleName} в†’ {newRoleName}." });
        }

        private async Task CreateCourierProfileInTransactionAsync(int userId, int companyId)
        {
            if (await _context.CourierProfiles.AnyAsync(c => c.User_id == userId))
                return;

            var defaultCategory = await _context.VehicleCategories.OrderBy(c => c.ID_Category).FirstOrDefaultAsync();
            var defaultSchedule = await _context.ScheduleTypes.OrderBy(s => s.ID_SheduleType).FirstOrDefaultAsync();
            if (defaultCategory == null || defaultSchedule == null)
                return;

            var defaultStatus = await _context.CourierStatuses.FirstOrDefaultAsync(s => s.Name == "РќРµ РЅР° СЃРјРµРЅРµ");
            if (defaultStatus == null)
            {
                defaultStatus = new CourierStatus { Name = "РќРµ РЅР° СЃРјРµРЅРµ", Description = "РљСѓСЂСЊРµСЂ РЅРµ РЅР° СЃРјРµРЅРµ" };
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
    }
}

