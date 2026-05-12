namespace WebBlazorDeliveryCRM.Services;

public class NotificationPopupService
{
    public event Action<PopupNotification>? Raised;

    public void Raise(PopupNotification notification)
    {
        Raised?.Invoke(notification);
    }

    public sealed record PopupNotification(int NotificationId, string Title, string Message, string TargetUrl);
}

