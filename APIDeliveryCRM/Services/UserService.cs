using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Services
{
    public class UserLoginService : IUserLoginService
    {
        private readonly ContextDB _context;
        private readonly IConfiguration _configuration;

        public UserLoginService(ContextDB context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.ID_User == id);
            if (user == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(user);
        }

        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
            return new OkObjectResult(users);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hashedPassword;
        }

        private string GenerateJwtToken(User user, string email)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID_User.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, $"{user.FName} {user.Name}"),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Unknown"),
                new Claim("company", user.Company?.Name ?? string.Empty),
                new Claim("companyId", user.Company_id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<IActionResult> RegisterClientAsync(RegisterClientRequest dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == dto.Email);
                if (existingLogin != null)
                {
                    return new BadRequestObjectResult(new { message = "Пользователь с таким email уже существует" });
                }

                // Получаем дефолтную компанию (ID = 1)
                var defaultCompany = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == 1);
                if (defaultCompany == null)
                {
                    defaultCompany = new Company
                    {
                        Name = "Default Company",
                        Subdomain = "default",
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        SubscriptionPlan = "Pro",
                        MaxUsers = 100,
                        MaxOrdersPerMonth = 10000,
                        SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                        SlaOnTimeHours = 4,
                        SlaLateHours = 24
                    };
                    _context.Companies.Add(defaultCompany);
                    await _context.SaveChangesAsync();
                }

                var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Клиент");
                if (clientRole == null)
                {
                    clientRole = new Role { Name = "Клиент" };
                    _context.Roles.Add(clientRole);
                    await _context.SaveChangesAsync();
                }

                var defaultPaymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync();
                if (defaultPaymentMethod == null)
                {
                    defaultPaymentMethod = new PaymentMethod { Name = "Наличные" };
                    _context.PaymentMethods.Add(defaultPaymentMethod);
                    await _context.SaveChangesAsync();
                }

                var login = new Login
                {
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    User = new User
                    {
                        FName = dto.FName,
                        Name = dto.Name,
                        Patronumic = dto.Patronumic,
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        Theme = "light",
                        Avatar = "/avatars/default.png",
                        Role_id = clientRole.ID_Role,
                        Role = clientRole,
                        Company_id = defaultCompany.ID_Company,
                        Company = defaultCompany
                    }
                };

                await _context.AddAsync(login);
                await _context.SaveChangesAsync();

                var clientProfile = new ClientProfile
                {
                    Company_id = defaultCompany.ID_Company,
                    Company = defaultCompany,
                    User_id = login.User.ID_User,
                    User = login.User,
                    Default_address = string.Empty,
                    Rating = 0,
                    Preferred_payment_method_id = defaultPaymentMethod.ID_PaymentMethod,
                    PaymentMethod = defaultPaymentMethod
                };

                await _context.AddAsync(clientProfile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,
                    userId = login.User.ID_User,
                    clientProfileId = clientProfile.ID_ClientProfile
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { message = $"Ошибка при регистрации: {ex.Message}" });
            }
        }

        public async Task<IActionResult> RegisterManagerAsync(RegisterManagerRequest dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == dto.Email);
                if (existingLogin != null)
                {
                    return new BadRequestObjectResult(new { message = "Пользователь с таким email уже существует" });
                }

                // Получаем дефолтную компанию (ID = 1)
                var defaultCompany = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == 1);
                if (defaultCompany == null)
                {
                    defaultCompany = new Company
                    {
                        Name = "Default Company",
                        Subdomain = "default",
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        SubscriptionPlan = "Pro",
                        MaxUsers = 100,
                        MaxOrdersPerMonth = 10000,
                        SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                        SlaOnTimeHours = 4,
                        SlaLateHours = 24
                    };
                    _context.Companies.Add(defaultCompany);
                    await _context.SaveChangesAsync();
                }

                var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Менеджер");
                if (managerRole == null)
                {
                    managerRole = new Role { Name = "Менеджер" };
                    _context.Roles.Add(managerRole);
                    await _context.SaveChangesAsync();
                }

                var login = new Login
                {
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    User = new User
                    {
                        FName = dto.FName,
                        Name = dto.Name,
                        Patronumic = dto.Patronumic,
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        Theme = "light",
                        Avatar = "/avatars/default.png",
                        Role_id = managerRole.ID_Role,
                        Role = managerRole,
                        Company_id = defaultCompany.ID_Company,
                        Company = defaultCompany
                    }
                };

                await _context.AddAsync(login);
                await _context.SaveChangesAsync();

                var managerProfile = new ManagerProfile
                {
                    Company_id = defaultCompany.ID_Company,
                    Company = defaultCompany,
                    User_id = login.User.ID_User,
                    User = login.User,
                    Position = dto.Position ?? "Рядовой менеджер",
                    Department = dto.Department,
                    Passport_series = dto.Passport_series,
                    Passport_number = dto.Passport_number,
                    Passport_issued_by = dto.Passport_issued_by,
                    Passport_issued_date = dto.Passport_issued_date,
                    Phone = dto.Phone,
                    HireDate = DateTime.UtcNow
                };

                await _context.AddAsync(managerProfile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,
                    userId = login.User.ID_User,
                    managerProfileId = managerProfile.ID_ManagerProfile
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { message = $"Ошибка при регистрации: {ex.Message}" });
            }
        }

        public async Task<IActionResult> RegisterLogisticianAsync(RegisterLogisticianRequest dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == dto.Email);
                if (existingLogin != null)
                {
                    return new BadRequestObjectResult(new { message = "Пользователь с таким email уже существует" });
                }

                // Получаем дефолтную компанию (ID = 1)
                var defaultCompany = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == 1);
                if (defaultCompany == null)
                {
                    defaultCompany = new Company
                    {
                        Name = "Default Company",
                        Subdomain = "default",
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        SubscriptionPlan = "Pro",
                        MaxUsers = 100,
                        MaxOrdersPerMonth = 10000,
                        SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                        SlaOnTimeHours = 4,
                        SlaLateHours = 24
                    };
                    _context.Companies.Add(defaultCompany);
                    await _context.SaveChangesAsync();
                }

                var logisticRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Логист");
                if (logisticRole == null)
                {
                    logisticRole = new Role { Name = "Логист" };
                    _context.Roles.Add(logisticRole);
                    await _context.SaveChangesAsync();
                }

                var login = new Login
                {
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    User = new User
                    {
                        FName = dto.FName,
                        Name = dto.Name,
                        Patronumic = dto.Patronumic,
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        Theme = "light",
                        Avatar = "/avatars/default.png",
                        Role_id = logisticRole.ID_Role,
                        Role = logisticRole,
                        Company_id = defaultCompany.ID_Company,
                        Company = defaultCompany
                    }
                };

                await _context.AddAsync(login);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,
                    userId = login.User.ID_User
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { message = $"Ошибка при регистрации: {ex.Message}" });
            }
        }

        public async Task<IActionResult> LoginAsync(LoginRequest dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return new BadRequestObjectResult(new { message = "Email и пароль обязательны" });
            }

            var login = await _context.Logins
                .Include(l => l.User)
                    .ThenInclude(u => u.Role)
                .Include(l => l.User)
                    .ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(l => l.Email == dto.Email);

            if (login == null || login.User == null)
            {
                return new UnauthorizedObjectResult(new { message = "Неверный email или пароль" });
            }

            if (!login.User.Is_Active)
            {
                return new UnauthorizedObjectResult(new { message = "Аккаунт деактивирован" });
            }

            if (!VerifyPassword(dto.Password, login.Password))
            {
                return new UnauthorizedObjectResult(new { message = "Неверный email или пароль" });
            }

            var token = GenerateJwtToken(login.User, login.Email);

            return new OkObjectResult(token);
        }

        public async Task<IActionResult> RegisterCourierAsync(RegisterCourierRequest dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.Email == dto.Email);
                if (existingLogin != null)
                {
                    return new BadRequestObjectResult(new { message = "Пользователь с таким email уже существует" });
                }

                // Получаем дефолтную компанию (ID = 1)
                var defaultCompany = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == 1);
                if (defaultCompany == null)
                {
                    defaultCompany = new Company
                    {
                        Name = "Default Company",
                        Subdomain = "default",
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        SubscriptionPlan = "Pro",
                        MaxUsers = 100,
                        MaxOrdersPerMonth = 10000,
                        SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                        SlaOnTimeHours = 4,
                        SlaLateHours = 24
                    };
                    _context.Companies.Add(defaultCompany);
                    await _context.SaveChangesAsync();
                }

                var courierRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Курьер");
                if (courierRole == null)
                {
                    courierRole = new Role { Name = "Курьер" };
                    _context.Roles.Add(courierRole);
                    await _context.SaveChangesAsync();
                }

                var vehicleCategory = await _context.VehicleCategories.FindAsync(dto.VehicleCategory_id);
                if (vehicleCategory == null)
                {
                    return new BadRequestObjectResult(new { message = "Категория транспорта не найдена" });
                }

                var scheduleType = await _context.ScheduleTypes.FindAsync(dto.WorkSchedule_id);
                if (scheduleType == null)
                {
                    return new BadRequestObjectResult(new { message = "Тип графика работы не найден" });
                }

                var defaultStatus = await _context.CourierStatuses.FirstOrDefaultAsync(s => s.Name == "Не на смене");
                if (defaultStatus == null)
                {
                    defaultStatus = new CourierStatus { Name = "Не на смене", Description = "Курьер не на смене" };
                    _context.CourierStatuses.Add(defaultStatus);
                    await _context.SaveChangesAsync();
                }

                var login = new Login
                {
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    User = new User
                    {
                        FName = dto.FName,
                        Name = dto.Name,
                        Patronumic = dto.Patronumic,
                        Created_at = DateTime.UtcNow,
                        Is_Active = true,
                        Theme = "light",
                        Avatar = "/avatars/default.png",
                        Role_id = courierRole.ID_Role,
                        Role = courierRole,
                        Company_id = defaultCompany.ID_Company,
                        Company = defaultCompany
                    }
                };

                await _context.AddAsync(login);
                await _context.SaveChangesAsync();

                var passportData = string.IsNullOrEmpty(dto.Passport_series) || string.IsNullOrEmpty(dto.Passport_number)
                    ? null
                    : $"{dto.Passport_series} {dto.Passport_number}";

                var courierProfile = new CourierProfile
                {
                    Company_id = defaultCompany.ID_Company,
                    Company = defaultCompany,
                    User_id = login.User.ID_User,
                    User = login.User,
                    VehicleCategory_id = dto.VehicleCategory_id,
                    VehicleCategory = vehicleCategory,
                    DriverLicense = dto.DriverLicense,
                    Passport_data = passportData,
                    WorkSchedule_id = dto.WorkSchedule_id,
                    ScheduleType = scheduleType,
                    CurrentStatus_id = defaultStatus.ID_CourierStatus,
                    CourierStatus = defaultStatus,
                    Total_deliveries = 0,
                    Current_lat = 0,
                    Current_lon = 0,
                    Rating = 0,
                    Is_online = false,
                    LastActivity_at = DateTime.UtcNow
                };

                await _context.AddAsync(courierProfile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,
                    userId = login.User.ID_User,
                    courierProfileId = courierProfile.ID_CourierProfile
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { message = $"Ошибка при регистрации: {ex.Message}" });
            }
        }

     


        public async Task<IActionResult> GetAllManagersAsync()
        {
            var managers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name == "Менеджер")
                .ToListAsync();

            return new OkObjectResult(managers);
        }

        public async Task<IActionResult> GetAllCourierAsync()
        {
            var couriers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name == "Курьер")
                .ToListAsync();
            
            return new OkObjectResult(couriers);
        }

        public async Task<IActionResult> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            if (!string.IsNullOrEmpty(request.FName))
            {
                user.FName = request.FName;
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                user.Name = request.Name;
            }

            if (request.Patronumic != null)
            {
                user.Patronumic = request.Patronumic;
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Данные пользователя успешно обновлены", user });
        }
    }
}


