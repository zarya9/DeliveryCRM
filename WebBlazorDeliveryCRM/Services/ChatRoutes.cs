namespace WebBlazorDeliveryCRM.Services;

public static class ChatRoutes
{
    public static string BasePathForRole(string? role) => role switch
    {
        "Клиент" => "/customer/chat",
        "Курьер" or "Система" => "/courier/chat",
        "Логист" or "Логистика" => "/logistician/chat",
        _ => "/manager/chat"
    };

    public static string Room(string? role, int chatRoomId) =>
        chatRoomId > 0 ? $"{BasePathForRole(role)}?roomId={chatRoomId}" : BasePathForRole(role);
}
