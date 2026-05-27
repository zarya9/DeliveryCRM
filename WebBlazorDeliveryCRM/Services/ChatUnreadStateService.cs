namespace WebBlazorDeliveryCRM.Services;

public sealed class ChatUnreadStateService
{
    private readonly ChatApiService _chatApi;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event Action? Changed;

    public int DirectUnreadCount { get; private set; }
    public bool HasDirectUnread => DirectUnreadCount > 0;

    public int TotalChatUnreadCount { get; private set; }
    public bool HasAnyChatUnread => TotalChatUnreadCount > 0;

    public ChatUnreadStateService(ChatApiService chatApi)
    {
        _chatApi = chatApi;
    }

    public async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        try
        {
            var (rooms, _) = await _chatApi.GetRoomsListAsync();
            var total = rooms.Sum(r => r.UnreadCount);
            var direct = rooms
                .Where(r => string.Equals(r.RoomKind, "direct", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.UnreadCount);

            if (total == TotalChatUnreadCount && direct == DirectUnreadCount)
                return;

            TotalChatUnreadCount = total;
            DirectUnreadCount = direct;
            Changed?.Invoke();
        }
        catch
        {
            // Intentionally ignored: chat badge should not break UI.
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
