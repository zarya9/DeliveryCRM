using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class AuthApiService
{
    public const string ServerUnavailableUserMessage = "Извините, ошибка на стороне сервера. Уже работаем.";

    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(IHttpClientFactory factory, IConfiguration configuration)
    {
        _factory = factory;
        _configuration = configuration;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient("UnauthorizedClient");
        var request = new { Email = email, Password = password };

        try
        {
            var response = await client.PostAsJsonAsync("/api/Users/Login", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync();
                var token = ParseTokenFromResponse(raw);
                if (string.IsNullOrEmpty(token))
                    return new LoginResult { Success = false, ErrorMessage = "Сервер вернул пустой токен." };
                return new LoginResult { Success = true, Token = token };
            }

            // Ошибка 400/401 — читаем message из тела
            var errorBody = await response.Content.ReadAsStringAsync();
            var message = "Неверный email или пароль.";
            if (!string.IsNullOrEmpty(errorBody))
            {
                try
                {
                    var doc = JsonDocument.Parse(errorBody);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        message = msgProp.GetString() ?? message;
                }
                catch { /* используем message по умолчанию */ }
            }
            return new LoginResult { Success = false, ErrorMessage = message };
        }
        catch (HttpRequestException)
        {
            return new LoginResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (TaskCanceledException)
        {
            return new LoginResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (Exception)
        {
            return new LoginResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
    }

    public async Task<RegistrationResult> RegisterClientAsync(RegisterClientUiRequest request)
    {
        return await RegisterAsync("/api/Users/RegisterClient", request);
    }

    public async Task<RegistrationResult> RegisterCompanyOwnerAsync(RegisterCompanyOwnerUiRequest request)
    {
        return await RegisterAsync("/api/Users/RegisterCompanyOwner", request);
    }

    private static string? ParseTokenFromResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        raw = raw.Trim();
        // Plain text JWT (начинается с eyJ)
        if (raw.StartsWith("eyJ", StringComparison.Ordinal))
            return raw;
        // JSON-строка в кавычках
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            return raw[1..^1].Replace("\\\"", "\"");
        try
        {
            return JsonSerializer.Deserialize<string>(raw, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RegistrationResult> RegisterAsync<T>(string url, T request)
    {
        var client = _factory.CreateClient("UnauthorizedClient");
        try
        {
            var response = await client.PostAsJsonAsync(url, request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return new RegistrationResult { Success = true };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            var message = "Не удалось зарегистрировать аккаунт.";
            if (!string.IsNullOrWhiteSpace(errorBody))
            {
                try
                {
                    var doc = JsonDocument.Parse(errorBody);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        message = msgProp.GetString() ?? message;
                }
                catch
                {
                    // ignore parsing error, keep default message
                }
            }

            return new RegistrationResult { Success = false, ErrorMessage = message };
        }
        catch (HttpRequestException)
        {
            return new RegistrationResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (TaskCanceledException)
        {
            return new RegistrationResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (Exception)
        {
            return new RegistrationResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
    }

    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email)
    {
        var client = _factory.CreateClient("UnauthorizedClient");
        try
        {
            var response = await client.PostAsJsonAsync("/api/Users/password-reset/request", new { Email = email }, JsonOptions);
            var body = await response.Content.ReadAsStringAsync();
            var message = ParseMessageFromBody(body) ?? "Если указанный email зарегистрирован в системе, на него отправлено письмо с кодом восстановления.";
            if (response.IsSuccessStatusCode)
                return new PasswordResetRequestResult { Success = true, Message = message };
            if ((int)response.StatusCode == 503)
                return new PasswordResetRequestResult { Success = false, ErrorMessage = message };
            return new PasswordResetRequestResult { Success = false, ErrorMessage = message };
        }
        catch (HttpRequestException)
        {
            return new PasswordResetRequestResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (TaskCanceledException)
        {
            return new PasswordResetRequestResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (Exception)
        {
            return new PasswordResetRequestResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
    }

    public async Task<PasswordResetCompleteResult> CompletePasswordResetAsync(string email, string code, string newPassword)
    {
        var client = _factory.CreateClient("UnauthorizedClient");
        try
        {
            var response = await client.PostAsJsonAsync("/api/Users/password-reset/complete",
                new { Email = email, Code = code, NewPassword = newPassword }, JsonOptions);
            var body = await response.Content.ReadAsStringAsync();
            var message = ParseMessageFromBody(body) ?? "Готово.";
            if (response.IsSuccessStatusCode)
                return new PasswordResetCompleteResult { Success = true, Message = message };
            return new PasswordResetCompleteResult { Success = false, ErrorMessage = message };
        }
        catch (HttpRequestException)
        {
            return new PasswordResetCompleteResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (TaskCanceledException)
        {
            return new PasswordResetCompleteResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
        catch (Exception)
        {
            return new PasswordResetCompleteResult { Success = false, ErrorMessage = ServerUnavailableUserMessage };
        }
    }

    private static string? ParseMessageFromBody(string errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString();
        }
        catch
        {
            return null;
        }

        return null;
    }
}

public sealed class PasswordResetRequestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class PasswordResetCompleteResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class RegisterClientUiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Patronumic { get; set; }
}

public sealed class RegisterCompanyOwnerUiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Patronumic { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string SubscriptionPlan { get; set; } = "Pro";
    public int MaxUsers { get; set; } = 100;
    public int MaxOrdersPerMonth { get; set; } = 10000;
    public int SlaOnTimeHours { get; set; } = 4;
    public int SlaLateHours { get; set; } = 24;
}

public sealed class RegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
