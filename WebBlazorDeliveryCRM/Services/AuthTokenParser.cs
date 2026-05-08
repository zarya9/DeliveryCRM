using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Globalization;

namespace WebBlazorDeliveryCRM.Services;

public static class AuthTokenParser
{
    public static ClaimsPrincipal? TryCreatePrincipal(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var identity = TryCreateIdentity(token);
        return identity is null ? null : new ClaimsPrincipal(identity);
    }

    public static ClaimsIdentity? TryCreateIdentity(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (IsExpired(jwt))
                return null;
            var mapped = jwt.Claims.Select(c =>
                IsRoleClaimType(c.Type)
                    ? new Claim(ClaimTypes.Role, c.Value)
                    : c).ToList();

            return new ClaimsIdentity(mapped, "jwt", ClaimTypes.Name, ClaimTypes.Role);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsRoleClaimType(string type)
    {
        if (string.IsNullOrEmpty(type))
            return false;
        if (type == ClaimTypes.Role)
            return true;
        if (type == "role" || type == "roles")
            return true;
        return type.EndsWith("/role", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpired(JwtSecurityToken jwt)
    {
        var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (string.IsNullOrWhiteSpace(exp))
            return false;
        if (!long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            return false;
        var expiresUtc = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return expiresUtc <= DateTimeOffset.UtcNow;
    }
}
