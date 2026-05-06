namespace WebBlazorDeliveryCRM.Services;

public sealed class AuthTokenCache
{
    private readonly object _sync = new();
    private string? _token;

    public string? GetToken()
    {
        lock (_sync) return _token;
    }

    public void SetToken(string? token)
    {
        lock (_sync)
            _token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    }

    public void Clear()
    {
        lock (_sync) _token = null;
    }
}
