using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>Состояние входа только из HttpOnly-cookie (читается на сервере из запроса).</summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _js;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, IJSRuntime js)
    {
        _httpContextAccessor = httpContextAccessor;
        _js = js;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName];
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var identity = GetIdentityFromToken(token);
        if (identity == null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    /// <summary>Сброс cookie через браузерный fetch и обновление UI (без localStorage).</summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("deliveryCrmAuth.clearSession");
        }
        catch
        {
            /* circuit / JS недоступен */
        }

        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static ClaimsIdentity? GetIdentityFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return new ClaimsIdentity(jwt.Claims, "jwt");
        }
        catch
        {
            return null;
        }
    }
}
