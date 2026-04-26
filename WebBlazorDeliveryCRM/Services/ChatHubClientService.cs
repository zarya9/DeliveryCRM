using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebBlazorDeliveryCRM.Services;

public class ChatHubClientService : IAsyncDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;

    public ChatHubClientService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public HubConnection? Connection => _hubConnection;

    public async Task<HubConnection> GetConnectionAsync()
    {
        if (_hubConnection != null)
            return _hubConnection;

        var apiBase = _configuration["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5220";
        var hubUrl = $"{apiBase}/chatHub";

        await Task.CompletedTask;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName];
                    return Task.FromResult<string?>(token);
                };
            })
            .WithAutomaticReconnect()
            .Build();

        return _hubConnection;
    }

    public async Task StartAsync()
    {
        var connection = await GetConnectionAsync();
        if (connection.State == HubConnectionState.Disconnected)
            await connection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
