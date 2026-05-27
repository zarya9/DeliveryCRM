using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Utilities;

public static class NotificationVisuals
{
    public sealed record Visual(string IconName, string Tone, string CategoryLabel);

    public static Visual Resolve(NotificationItemDto n)
    {
        var type = (n.TypeName ?? string.Empty).Trim();
        var title = (n.Title ?? string.Empty).Trim();
        var combined = $"{type} {title}".ToLowerInvariant();

        if (type.Contains("chat", StringComparison.OrdinalIgnoreCase)
            || title.Contains("сообщен", StringComparison.OrdinalIgnoreCase))
            return new Visual("message-01", "chat", "Чат");

        if (type.Contains("sla", StringComparison.OrdinalIgnoreCase)
            || title.Contains("sla", StringComparison.OrdinalIgnoreCase)
            || title.Contains("риск", StringComparison.OrdinalIgnoreCase))
            return new Visual("notification-01", "sla", "SLA");

        if (type.Contains("начало смены", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("shift_started")
            || title.Contains("начал смену", StringComparison.OrdinalIgnoreCase))
            return new Visual("calendar-03", "shift", "Смена");

        if (type.Contains("завершение смены", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("shift_finished")
            || title.Contains("завершил смену", StringComparison.OrdinalIgnoreCase))
            return new Visual("route-02", "shift", "Смена");

        if (n.ShiftId.HasValue
            || type.Contains("смен", StringComparison.OrdinalIgnoreCase)
            || title.Contains("смен", StringComparison.OrdinalIgnoreCase))
            return new Visual("delivery-truck-01", "shift", "Смена");

        if (n.OrderId.HasValue
            || type.Contains("заказ", StringComparison.OrdinalIgnoreCase)
            || title.Contains("заказ", StringComparison.OrdinalIgnoreCase))
            return new Visual("invoice-01", "order", "Заказ");

        if (type.Contains("тикет", StringComparison.OrdinalIgnoreCase)
            || type.Contains("ticket", StringComparison.OrdinalIgnoreCase)
            || title.Contains("обращен", StringComparison.OrdinalIgnoreCase))
            return new Visual("task-01", "ticket", "Поддержка");

        if (type.Contains("оплат", StringComparison.OrdinalIgnoreCase)
            || type.Contains("billing", StringComparison.OrdinalIgnoreCase)
            || title.Contains("оплат", StringComparison.OrdinalIgnoreCase))
            return new Visual("wallet-01", "billing", "Оплата");

        if (n.IsCritical)
            return new Visual("notification-01", "critical", "Важное");

        return new Visual("notification-01", "system", "Система");
    }

    public static string? ResolveTargetUrl(NotificationItemDto n, string? role)
    {
        if (n.ShiftId is > 0)
            return $"/logistician/shifts/{n.ShiftId.Value}";

        if (n.OrderId is > 0)
        {
            return role switch
            {
                "Клиент" => $"/customer/tracking",
                "Курьер" or "Система" => "/courier/orders",
                "Логист" or "Логистика" => "/logistician",
                _ => "/manager/orders"
            };
        }

        return null;
    }
}
