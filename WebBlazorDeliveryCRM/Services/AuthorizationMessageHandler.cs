using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Scoped handler: при CreateClient из scoped-сервиса Blazor circuit получает JWT из того же circuit scope.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitAuthPrincipalHolder _circuitHolder;
    private readonly AuthTokenCache _tokenCache;
    private readonly ILogger<AuthorizationMessageHandler> _logger;

    public AuthorizationMessageHandler(
        IHttpContextAccessor httpContextAccessor,
        CircuitAuthPrincipalHolder circuitHolder,
        AuthTokenCache tokenCache,
        ILogger<AuthorizationMessageHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitHolder = circuitHolder;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = AuthSessionTokenResolver.Resolve(_httpContextAccessor, _circuitHolder, _tokenCache);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("AuthorizedClient: Bearer token attached for {Method} {Uri}", request.Method, request.RequestUri);
        }
        else
        {
            _logger.LogWarning("AuthorizedClient: no JWT token for {Method} {Uri}", request.Method, request.RequestUri);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
