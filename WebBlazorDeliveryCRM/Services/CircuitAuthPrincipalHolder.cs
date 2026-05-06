using System.Security.Claims;

namespace WebBlazorDeliveryCRM.Services;

public sealed class CircuitAuthPrincipalHolder
{
    private ClaimsPrincipal? _principal;
    private string? _jwt;

    public ClaimsPrincipal? Principal => _principal;

    public string? JwtToken => _jwt;

    public void SetAuth(string? jwt, ClaimsPrincipal? principal)
    {
        _jwt = string.IsNullOrWhiteSpace(jwt) ? null : jwt.Trim();
        _principal = principal;
    }

    public void SetPrincipal(ClaimsPrincipal? principal) => _principal = principal;

    public void Clear()
    {
        _principal = null;
        _jwt = null;
    }
}
