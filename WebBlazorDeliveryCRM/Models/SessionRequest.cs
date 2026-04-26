namespace WebBlazorDeliveryCRM.Models;

/// <summary>Тело POST /api/auth/session — токен после успешного логина к API (однократная передача в браузер для записи в HttpOnly-cookie).</summary>
public sealed class SessionRequest
{
    public string? Token { get; set; }
}
