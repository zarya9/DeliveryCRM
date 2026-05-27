namespace APIDeliveryCRM.Interfaces;

public interface IUserPresenceService
{
    void UserConnected(int userId);
    void UserDisconnected(int userId);
    IReadOnlyCollection<int> GetOnlineUserIds();
    bool IsUserOnline(int userId);
    void SetViewingRoom(int userId, int chatRoomId);
    void ClearViewingRoom(int userId, int chatRoomId);
    bool IsViewingRoom(int userId, int chatRoomId);
}
