using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WebBlazorDeliveryCRM.Services;

public sealed class AuthCircuitHandler : CircuitHandler
{
    private readonly IHttpContextAccessor _http;
    private readonly CircuitAuthPrincipalHolder _holder;
    private readonly AuthTokenCache _tokenCache;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthCircuitHandler(
        IHttpContextAccessor http,
        CircuitAuthPrincipalHolder holder,
        AuthTokenCache tokenCache,
        CustomAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _holder = holder;
        _tokenCache = tokenCache;
        _authStateProvider = authStateProvider;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var token = _http.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName];
        var principal = AuthTokenParser.TryCreatePrincipal(token);
        if (principal?.Identity?.IsAuthenticated == true)
        {
            _holder.SetAuth(token, principal);
            _tokenCache.SetToken(token);
            _authStateProvider.NotifyAuthFromCircuit();
        }

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _holder.Clear();
        return Task.CompletedTask;
    }
}
