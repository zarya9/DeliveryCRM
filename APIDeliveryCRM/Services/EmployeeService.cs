using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ContextDB _context;

        public EmployeeService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId)
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Company_id == companyId)
                .OrderBy(u => u.FName)
                .ThenBy(u => u.Name)
                .Select(u => new
                {
                    id = u.ID_User,
                    fullName = $"{u.FName} {u.Name} {u.Patronumic}".Trim(),
                    u.Patronumic,
                    role = u.Role.Name,
                    u.Is_Active,
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
                    Password = HashPassword(request.Password),
                    User = user
                };

                _context.Logins.Add(login);
                await _context.SaveChangesAsync();
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

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

