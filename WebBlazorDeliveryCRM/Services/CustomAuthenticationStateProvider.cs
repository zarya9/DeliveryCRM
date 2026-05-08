using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace WebBlazorDeliveryCRM.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitAuthPrincipalHolder _circuitHolder;
    private readonly AuthTokenCache _tokenCache;
    private readonly IJSRuntime _js;

    public CustomAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        CircuitAuthPrincipalHolder circuitHolder,
        AuthTokenCache tokenCache,
        IJSRuntime js)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitHolder = circuitHolder;
        _tokenCache = tokenCache;
        _js = js;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var http = _httpContextAccessor.HttpContext;
        var token = http?.Request.Cookies[AuthCookieConstants.CookieName];

        // Тут важный момент: если cookie уже есть, сразу собираем principal и кэшируем в circuit.
        if (!string.IsNullOrEmpty(token))
        {
            var principal = AuthTokenParser.TryCreatePrincipal(token);
            if (principal?.Identity?.IsAuthenticated == true)
            {
                _circuitHolder.SetAuth(token, principal);
                _tokenCache.SetToken(token);
                return Task.FromResult(new AuthenticationState(principal));
            }

            _circuitHolder.Clear();
            _tokenCache.Clear();
            return Task.FromResult(Anonymous());
        }

        // Короче, это обычный HTTP-запрос без cookie: считаем пользователя анонимным.
        if (http is not null)
        {
            _circuitHolder.Clear();
            _tokenCache.Clear();
            return Task.FromResult(Anonymous());
        }

        // Здесь работаем через SignalR-циркит, чтобы не терять вход между интерактивными рендерами.
        var cached = _circuitHolder.Principal;
        if (cached?.Identity?.IsAuthenticated == true)
            return Task.FromResult(new AuthenticationState(cached));

        return Task.FromResult(Anonymous());
    }

    public void NotifyAuthFromCircuit() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public Task MarkUserAsAuthenticatedAsync(string token)
    {
        var principal = AuthTokenParser.TryCreatePrincipal(token);
        if (principal?.Identity?.IsAuthenticated == true)
        {
            _circuitHolder.SetAuth(token, principal);
            _tokenCache.SetToken(token);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }
        else
        {
            _circuitHolder.Clear();
            _tokenCache.Clear();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        }

        return Task.CompletedTask;
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        _circuitHolder.Clear();
        _tokenCache.Clear();
        try
        {
            await _js.InvokeVoidAsync("deliveryCrmAuth.clearSession");
        }
        catch
        {
            /* circuit / JS недоступен */
        }

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
