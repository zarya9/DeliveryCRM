using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIDeliveryCRM.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ContextDB _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IPasswordHasher<Login> _passwordHasher = new PasswordHasher<Login>();

    public PasswordResetService(
        ContextDB context,
        IEmailSender emailSender,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IActionResult> RequestResetCodeAsync(RequestPasswordResetRequest dto, CancellationToken cancellationToken = default)
    {
        var emailRaw = dto.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(emailRaw) || !EmailRegex.IsMatch(emailRaw))
        {
            return new BadRequestObjectResult(new { message = "Укажите корректный email." });
        }

        var emailKey = emailRaw.ToLowerInvariant();
        var cooldownSeconds = Math.Clamp(_configuration.GetValue("PasswordReset:RequestCooldownSeconds", 90), 10, 3600);
        var cooldownKey = "pwd_reset_cd:" + emailKey;
        if (_memoryCache.TryGetValue(cooldownKey, out _))
        {
            return new OkObjectResult(new
            {
                message = "Если указанный email зарегистрирован в системе, на него отправлено письмо с кодом восстановления."
            });
        }

        _memoryCache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cooldownSeconds)
        });

        var login = await _context.Logins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Email.ToLower() == emailKey, cancellationToken)
            .ConfigureAwait(false);

        if (login?.User is null || !login.User.Is_Active)
        {
            return new OkObjectResult(new
            {
                message = "Если указанный email зарегистрирован в системе, на него отправлено письмо с кодом восстановления."
            });
        }

        var code = GenerateNumericCode();
        var codeHash = HashCode(code);
        var lifetimeMinutes = Math.Clamp(_configuration.GetValue("PasswordReset:CodeLifetimeMinutes", 15), 5, 120);
        var now = DateTime.UtcNow;

        var body =
            $"Здравствуйте!\n\n" +
            $"Код для восстановления пароля в DeliveryCRM: {code}\n\n" +
            $"Код действителен {lifetimeMinutes} минут.\n" +
            "Если вы не запрашивали сброс пароля, проигнорируйте это письмо.\n";

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var pending = await _context.PasswordResetCodes
                .Where(c => c.LoginId == login.ID_Login && c.ConsumedUtc == null && c.ExpiresUtc > now)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var row in pending)
                row.ConsumedUtc = now;

            _context.PasswordResetCodes.Add(new PasswordResetCode
            {
                LoginId = login.ID_Login,
                CodeHash = codeHash,
                CreatedUtc = now,
                ExpiresUtc = now.AddMinutes(lifetimeMinutes)
            });

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _emailSender.SendAsync(login.Email, "Восстановление пароля DeliveryCRM", body, cancellationToken)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Сброс пароля: не удалось сохранить код или отправить письмо на {Email}", login.Email);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _memoryCache.Remove(cooldownKey);
            return new ObjectResult(new { message = "Не удалось отправить письмо. Попробуйте позже или обратитесь к администратору." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        return new OkObjectResult(new
        {
            message = "Если указанный email зарегистрирован в системе, на него отправлено письмо с кодом восстановления."
        });
    }

    public async Task<IActionResult> CompleteResetAsync(CompletePasswordResetRequest dto, CancellationToken cancellationToken = default)
    {
        var emailRaw = dto.Email?.Trim() ?? string.Empty;
        var codeDigits = NormalizeCodeDigits(dto.Code);
        var newPassword = dto.NewPassword ?? string.Empty;

        if (string.IsNullOrEmpty(emailRaw) || !EmailRegex.IsMatch(emailRaw))
            return new BadRequestObjectResult(new { message = "Укажите корректный email." });
        if (codeDigits.Length < 6)
            return new BadRequestObjectResult(new { message = "Введите код из письма (6 цифр)." });
        if (newPassword.Length < 6)
            return new BadRequestObjectResult(new { message = "Пароль не короче 6 символов." });

        var emailKey = emailRaw.ToLowerInvariant();
        var login = await _context.Logins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Email.ToLower() == emailKey, cancellationToken)
            .ConfigureAwait(false);

        if (login?.User is null || !login.User.Is_Active)
            return new BadRequestObjectResult(new { message = "Неверный email, код или срок действия кода." });

        var codeHash = HashCode(codeDigits);
        var now = DateTime.UtcNow;

        var row = await _context.PasswordResetCodes
            .Where(c => c.LoginId == login.ID_Login && c.ConsumedUtc == null && c.ExpiresUtc > now && c.CodeHash == codeHash)
            .OrderByDescending(c => c.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return new BadRequestObjectResult(new { message = "Неверный email, код или срок действия кода." });

        row.ConsumedUtc = now;
        login.Password = _passwordHasher.HashPassword(login, newPassword);

        var others = await _context.PasswordResetCodes
            .Where(c => c.LoginId == login.ID_Login && c.ConsumedUtc == null && c.Id != row.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var o in others)
            o.ConsumedUtc = now;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OkObjectResult(new { message = "Пароль успешно изменён. Можно войти с новым паролем." });
    }

    private static string GenerateNumericCode()
    {
        var n = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return n.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeCodeDigits(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;
        var sb = new StringBuilder(code.Length);
        foreach (var ch in code)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }
}
