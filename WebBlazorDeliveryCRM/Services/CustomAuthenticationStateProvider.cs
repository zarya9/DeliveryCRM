using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebBlazorDeliveryCRM.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";
    private readonly ILocalStorageService _localStorage;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token;
        try
        {
            token = await _localStorage.GetItemAsync<string>(TokenKey);
        }
        catch (InvalidOperationException)
        {
            // Prerendering: JS interop unavailable — assume not authenticated; state will be re-evaluated when interactive.
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = GetIdentityFromToken(token);
        if (identity == null)
        {
            try { await _localStorage.RemoveItemAsync(TokenKey); } catch { /* ignore during prerender */ }
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsync(TokenKey, token);
        var identity = GetIdentityFromToken(token);
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(identity!))));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static ClaimsIdentity? GetIdentityFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return identity;
        }
        catch
        {
            return null;
        }
    }
}
