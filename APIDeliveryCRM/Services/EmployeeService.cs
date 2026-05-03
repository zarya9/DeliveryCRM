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

                if (string.Equals(role.Name, "Курьер", StringComparison.Ordinal))
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

        /// <summary>Создаёт CourierProfile в той же транзакции, что и сотрудник (роль «Курьер»).</summary>
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
    }
}

