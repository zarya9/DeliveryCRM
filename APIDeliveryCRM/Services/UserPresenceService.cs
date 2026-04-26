using System.Collections.Concurrent;
using APIDeliveryCRM.Interfaces;

namespace APIDeliveryCRM.Services;

public class UserPresenceService : IUserPresenceService
{
    private readonly ConcurrentDictionary<int, int> _connectionCount = new();

    public void UserConnected(int userId)
    {
        if (userId <= 0) return;
        _connectionCount.AddOrUpdate(userId, 1, (_, n) => n + 1);
    }

    public void UserDisconnected(int userId)
    {
        if (userId <= 0) return;
        while (true)
        {
            if (!_connectionCount.TryGetValue(userId, out var count))
                return;
            if (count <= 1)
            {
                _connectionCount.TryRemove(userId, out _);
                return;
            }

            if (_connectionCount.TryUpdate(userId, count - 1, count))
                return;
        }
    }

    public IReadOnlyCollection<int> GetOnlineUserIds()
    {
        return _connectionCount.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
    }

    public bool IsUserOnline(int userId)
    {
        return userId > 0 && _connectionCount.TryGetValue(userId, out var n) && n > 0;
    }
}
