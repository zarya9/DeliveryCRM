using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(IHttpClientFactory factory)
    {
        _factory = factory;
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
        catch (HttpRequestException ex)
        {
            return new LoginResult { Success = false, ErrorMessage = "Не удалось подключиться к API. Запустите проект APIDeliveryCRM (профиль «http», порт 5220) и обновите страницу. " + ex.Message };
        }
        catch (TaskCanceledException)
        {
            return new LoginResult { Success = false, ErrorMessage = "Превышено время ожидания (5 с). Запустите API (http://localhost:5220) и попробуйте снова." };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// API может вернуть токен как JSON-строку ("eyJ...") или как plain text (eyJ...).
    /// </summary>
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
}
