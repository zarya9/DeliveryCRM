namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Состояние открытого чата в circuit — чтобы не дублировать toast/popup, пока пользователь в комнате.
/// </summary>
public sealed class ChatNotificationContextService
{
    private bool _onChatPage;
    private int? _activeRoomId;

    public void SetOnChatPage(bool onChatPage) => _onChatPage = onChatPage;

    public void SetActiveRoomId(int? roomId) =>
        _activeRoomId = roomId is > 0 ? roomId : null;

    public bool ShouldNotifyForRoom(int chatRoomId) =>
        !(_onChatPage && _activeRoomId == chatRoomId);
}
