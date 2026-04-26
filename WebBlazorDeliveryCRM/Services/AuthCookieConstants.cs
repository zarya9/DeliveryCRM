namespace WebBlazorDeliveryCRM.Services;

/// <summary>Имя HttpOnly-cookie с JWT (только сервер и браузер при запросах, не доступен из JS).</summary>
public static class AuthCookieConstants
{
    public const string CookieName = "auth_token";
}
