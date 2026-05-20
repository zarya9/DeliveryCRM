using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ChatHubClientService : IAsyncDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitAuthPrincipalHolder _circuitHolder;
    private readonly AuthTokenCache _tokenCache;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<int, byte> _joinedRoomIds = new();
    private HubConnection? _hubConnection;
    private bool _handlersRegistered;

    public ChatHubClientService(
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

    public event Func<ChatIncomingMessage, Task>? MessageReceived;
    public event Func<MessageEditedSignal, Task>? MessageEdited;
    public event Func<int, Task>? MessageDeleted;
    public event Func<UserPresenceChangedDto, Task>? UserPresenceChanged;
    public event Func<Task>? Reconnected;

    public async Task<HubConnection> GetConnectionAsync()
    {
        if (_hubConnection != null)
            return _hubConnection;

        var apiBase = _configuration["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5220";
        var hubUrl = $"{apiBase}/chatHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieConstants.CookieName]
                                ?? _circuitHolder.JwtToken
                                ?? _tokenCache.GetToken();
                    if (string.IsNullOrWhiteSpace(token))
                        return Task.FromResult<string?>(null);
                    if (AuthTokenParser.TryCreatePrincipal(token)?.Identity?.IsAuthenticated != true)
                        return Task.FromResult<string?>(null);
                    return Task.FromResult<string?>(token);
                };
            })
            .AddJsonProtocol()
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hubConnection.Reconnected += OnReconnectedAsync;
        RegisterHandlers(_hubConnection);

        return _hubConnection;
    }

    public async Task StartAsync()
    {
        var connection = await GetConnectionAsync();
        if (connection.State == HubConnectionState.Disconnected)
            await connection.StartAsync();
    }

    public async Task JoinRoomAsync(int chatRoomId)
    {
        if (chatRoomId <= 0)
            return;

        _joinedRoomIds[chatRoomId] = 0;
        var connection = await GetConnectionAsync();
        await StartAsync();
        if (connection.State == HubConnectionState.Connected)
            await connection.SendAsync("JoinRoom", chatRoomId);
    }

    public async Task LeaveRoomAsync(int chatRoomId)
    {
        if (chatRoomId <= 0)
            return;

        _joinedRoomIds.TryRemove(chatRoomId, out _);
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.SendAsync("LeaveRoom", chatRoomId);
        }
        catch
        {
            // ignore disconnect races
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
            await _hubConnection.StopAsync();
    }

    private void RegisterHandlers(HubConnection connection)
    {
        if (_handlersRegistered)
            return;
        _handlersRegistered = true;

        connection.On<ChatIncomingMessageWire>("ReceiveMessage", wire =>
        {
            var msg = MapIncoming(wire);
            return DispatchAsync(MessageReceived, msg);
        });

        connection.On<MessageEditedSignal>("MessageEdited", signal =>
            DispatchAsync(MessageEdited, signal));

        connection.On<MessageDeletedSignal>("MessageDeleted", signal =>
        {
            if (MessageDeleted is null)
                return Task.CompletedTask;
            return DispatchAsync(MessageDeleted, signal.messageId);
        });

        connection.On<UserPresenceChangedDto>("UserPresenceChanged", payload =>
            DispatchAsync(UserPresenceChanged, payload));
    }

    private async Task OnReconnectedAsync(string? _)
    {
        var connection = _hubConnection;
        if (connection?.State != HubConnectionState.Connected)
            return;

        foreach (var roomId in _joinedRoomIds.Keys)
        {
            try
            {
                await connection.SendAsync("JoinRoom", roomId);
            }
            catch
            {
                // ignore per-room join failures
            }
        }

        if (Reconnected != null)
            await Reconnected.Invoke();
    }

    private static ChatIncomingMessage MapIncoming(ChatIncomingMessageWire wire) => new()
    {
        Id = wire.id,
        ChatRoomId = wire.chatRoomId,
        SenderId = wire.senderId,
        SenderName = wire.senderName,
        MessageText = wire.messageText ?? "",
        AttachmentUrl = wire.attachmentUrl,
        SentAt = wire.sentAt == default ? DateTime.UtcNow : wire.sentAt,
        EditedAt = wire.editedAt,
        IsDeleted = wire.isDeleted
    };

    private static async Task DispatchAsync<T>(Func<T, Task>? handlers, T payload)
    {
        if (handlers is null)
            return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<T, Task>>())
            await handler(payload);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            _hubConnection.Reconnected -= OnReconnectedAsync;
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _handlersRegistered = false;
        _joinedRoomIds.Clear();
    }

    private sealed class ChatIncomingMessageWire
    {
        public int id { get; set; }
        public int chatRoomId { get; set; }
        public int senderId { get; set; }
        public string? senderName { get; set; }
        public string? messageText { get; set; }
        public string? attachmentUrl { get; set; }
        public DateTime sentAt { get; set; }
        public DateTime? editedAt { get; set; }
        public bool isDeleted { get; set; }
    }

    public sealed class MessageEditedSignal
    {
        public int messageId { get; set; }
        public string? newText { get; set; }
        public DateTime? editedAt { get; set; }
    }

    private sealed class MessageDeletedSignal
    {
        public int messageId { get; set; }
    }
}
