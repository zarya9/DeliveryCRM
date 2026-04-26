namespace APIDeliveryCRM.Interfaces;

public interface IUserPresenceService
{
    void UserConnected(int userId);
    void UserDisconnected(int userId);
    IReadOnlyCollection<int> GetOnlineUserIds();
    /// <summary>Есть ли хотя бы одно активное SignalR-подключение пользователя.</summary>
    bool IsUserOnline(int userId);
}
