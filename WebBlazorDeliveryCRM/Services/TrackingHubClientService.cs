using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Подключение к SignalR /trackingHub для live-координат курьеров на карте мониторинга.
/// </summary>
public sealed class TrackingHubClientService : IAsyncDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitAuthPrincipalHolder _circuitHolder;
    private readonly AuthTokenCache _tokenCache;
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;

    public TrackingHubClientService(
        IHttpContextAccessor httpContextAccessor,
        CircuitAuthPrincipalHolder circuitHolder,
        AuthTokenCache tokenCache,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitHolder = circuitHolder;
        _tokenCache = tokenCache;
        _configuration = configuration;
    }

    public HubConnection? Connection => _hubConnection;

    public Task<HubConnection> GetConnectionAsync()
    {
        if (_hubConnection != null)
            return Task.FromResult(_hubConnection);

        var apiBase = _configuration["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5220";
        var hubUrl = $"{apiBase}/trackingHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    var token = AuthSessionTokenResolver.Resolve(_httpContextAccessor, _circuitHolder, _tokenCache);
                    if (string.IsNullOrWhiteSpace(token))
                        return Task.FromResult<string?>(null);
                    if (AuthTokenParser.TryCreatePrincipal(token)?.Identity?.IsAuthenticated != true)
                        return Task.FromResult<string?>(null);
                    return Task.FromResult<string?>(token);
                };
            })
            .WithAutomaticReconnect()
            .Build();

        return Task.FromResult(_hubConnection);
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        if (connection.State == HubConnectionState.Disconnected)
            await connection.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
        _hubConnection = null;
    }
}
