namespace APIDeliveryCRM.Interfaces;

public interface IUserPresenceService
{
    void UserConnected(int userId);
    void UserDisconnected(int userId);
    IReadOnlyCollection<int> GetOnlineUserIds();
    bool IsUserOnline(int userId);
}
