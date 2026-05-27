using Microsoft.AspNetCore.Http;

namespace WebBlazorDeliveryCRM.Services;

public static class AuthSessionTokenResolver
{
    public static string? Resolve(
        IHttpContextAccessor? httpContextAccessor,
        CircuitAuthPrincipalHolder? circuitHolder,
        AuthTokenCache? tokenCache)
    {
        var fromCookie = httpContextAccessor?.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName];
        if (!string.IsNullOrWhiteSpace(fromCookie))
            return fromCookie.Trim();

        var fromCircuit = circuitHolder?.JwtToken;
        if (!string.IsNullOrWhiteSpace(fromCircuit))
            return fromCircuit.Trim();

        return tokenCache?.GetToken();
    }
}
