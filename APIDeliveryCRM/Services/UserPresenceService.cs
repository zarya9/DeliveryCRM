using System.Collections.Concurrent;
using APIDeliveryCRM.Interfaces;

namespace APIDeliveryCRM.Services;

public class UserPresenceService : IUserPresenceService
{
    private readonly ConcurrentDictionary<int, int> _connectionCount = new();
    private readonly ConcurrentDictionary<int, int> _viewingRoomByUser = new();

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
            {
                _viewingRoomByUser.TryRemove(userId, out _);
                return;
            }
            if (count <= 1)
            {
                _connectionCount.TryRemove(userId, out _);
                _viewingRoomByUser.TryRemove(userId, out _);
                return;
            }

            if (_connectionCount.TryUpdate(userId, count - 1, count))
                return;
        }
    }

    public void SetViewingRoom(int userId, int chatRoomId)
    {
        if (userId <= 0 || chatRoomId <= 0) return;
        _viewingRoomByUser[userId] = chatRoomId;
    }

    public void ClearViewingRoom(int userId, int chatRoomId)
    {
        if (userId <= 0) return;
        if (_viewingRoomByUser.TryGetValue(userId, out var current) && current == chatRoomId)
            _viewingRoomByUser.TryRemove(userId, out _);
    }

    public bool IsViewingRoom(int userId, int chatRoomId) =>
        userId > 0 && chatRoomId > 0 &&
        _viewingRoomByUser.TryGetValue(userId, out var room) && room == chatRoomId;

    public IReadOnlyCollection<int> GetOnlineUserIds()
    {
        return _connectionCount.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
    }

    public bool IsUserOnline(int userId)
    {
        return userId > 0 && _connectionCount.TryGetValue(userId, out var n) && n > 0;
    }
}
