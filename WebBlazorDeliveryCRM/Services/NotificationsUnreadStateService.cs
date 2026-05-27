namespace WebBlazorDeliveryCRM.Services;

public sealed class NotificationsUnreadStateService
{
    private readonly NotificationsApiService _api;

    public NotificationsUnreadStateService(NotificationsApiService api)
    {
        _api = api;
    }

    public int Count { get; private set; }

    public event Action<int>? Changed;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var next = await _api.GetUnreadCountAsync(cancellationToken);
        if (next == Count) return;
        Count = next;
        Changed?.Invoke(Count);
    }

    public void SetIfDifferent(int next)
    {
        if (next == Count) return;
        Count = next;
        Changed?.Invoke(Count);
    }
}
