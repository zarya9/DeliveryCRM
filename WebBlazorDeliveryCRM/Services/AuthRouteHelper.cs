namespace WebBlazorDeliveryCRM.Services;

public static class AuthRouteHelper
{
    public static string GetPathOnly(string uri)
    {
        var relative = uri;
        if (uri.Contains("://", StringComparison.Ordinal))
        {
            try
            {
                var u = new Uri(uri);
                relative = u.AbsolutePath.TrimStart('/');
            }
            catch
            {
                relative = uri.TrimStart('/');
            }
        }
        else
        {
            relative = uri.TrimStart('/');
        }

        var pathOnly = relative.Split('?', '#')[0];
        var path = "/" + pathOnly.TrimStart('/');
        return string.IsNullOrEmpty(path) || path == "/" ? "/" : path;
    }

    public static bool CanAccessPathForRole(string? role, string path)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        var p = path.TrimEnd('/').ToLowerInvariant();
        if (p.Length == 0)
            p = "/";

        if (p.StartsWith("/courier", StringComparison.Ordinal))
            return role is "Курьер" or "Система" or "Администратор" or "Админ" or "Менеджер";
        if (p.StartsWith("/customer", StringComparison.Ordinal))
            return role == "Клиент";
        if (p.StartsWith("/logistician", StringComparison.Ordinal))
            return role is "Логист" or "Логистика";
        if (p.StartsWith("/manager", StringComparison.Ordinal))
            return role is "Менеджер" or "Админ" or "Администратор";
        if (p.StartsWith("/admin", StringComparison.Ordinal))
            return role is "Админ" or "Администратор";

        return true;
    }

    public static string GetDefaultPathForRole(string? role) => role switch
    {
        "Клиент" => "/customer",
        "Менеджер" => "/manager",
        "Логист" or "Логистика" => "/logistician",
        "Админ" or "Администратор" => "/admin/employees",
        "Курьер" or "Система" => "/courier/shift",
        _ => "/home"
    };

    public static bool IsAuthPage(string path)
    {
        var p = path.TrimEnd('/').ToLowerInvariant();
        if (p.Length == 0)
            p = "/";
        return p is "/" or "/login" or "/access-denied" or "/forgot-password";
    }
}
