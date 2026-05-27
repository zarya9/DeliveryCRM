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

        // HttpContext есть, но cookie в этом запросе нет (часть SignalR / сразу после входа).
        // Не сбрасываем circuit, пока в holder/cache ещё валидный JWT.
        if (http is not null)
        {
            var wireToken = AuthSessionTokenResolver.Resolve(_httpContextAccessor, _circuitHolder, _tokenCache);
            if (!string.IsNullOrEmpty(wireToken))
            {
                var fromWire = AuthTokenParser.TryCreatePrincipal(wireToken);
                if (fromWire?.Identity?.IsAuthenticated == true)
                {
                    _circuitHolder.SetAuth(wireToken, fromWire);
                    _tokenCache.SetToken(wireToken);
                    return Task.FromResult(new AuthenticationState(fromWire));
                }
            }

            _circuitHolder.Clear();
            _tokenCache.Clear();
            return Task.FromResult(Anonymous());
        }

        // SignalR-circuit: principal уже в holder — синхронизируем кэш для HttpClient.
        var cached = _circuitHolder.Principal;
        if (cached?.Identity?.IsAuthenticated == true)
        {
            var jwt = _circuitHolder.JwtToken;
            if (!string.IsNullOrEmpty(jwt))
                _tokenCache.SetToken(jwt);
            return Task.FromResult(new AuthenticationState(cached));
        }

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
