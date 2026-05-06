using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace WebBlazorDeliveryCRM.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitAuthPrincipalHolder _circuitHolder;
    private readonly AuthTokenCache _tokenCache;

    public AuthorizationMessageHandler(
        IHttpContextAccessor httpContextAccessor,
        CircuitAuthPrincipalHolder circuitHolder,
        AuthTokenCache tokenCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitHolder = circuitHolder;
        _tokenCache = tokenCache;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName]
                    ?? _circuitHolder.JwtToken
                    ?? _tokenCache.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
