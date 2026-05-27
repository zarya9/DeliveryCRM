namespace WebBlazorDeliveryCRM.Utilities;

/// <summary>Тон StatusBadge по названию статуса заказа.</summary>
public static class OrderStatusUi
{
    public static bool IsCancelled(string? statusName)
    {
        var s = (statusName ?? string.Empty).Trim();
        if (s.Length == 0)
            return false;
        return s.Contains("отмен", StringComparison.OrdinalIgnoreCase)
               || s.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDelivered(string? statusName, DateTime? deliveredAt = null)
    {
        if (deliveredAt.HasValue)
            return true;
        var s = (statusName ?? string.Empty).Trim();
        return s.Contains("доставлен", StringComparison.OrdinalIgnoreCase)
               || s.Equals("delivered", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveForClient(string? status, DateTime? deliveredAt = null)
        => !IsCancelled(status) && !IsDelivered(status, deliveredAt);

    public static string ToneForStatusName(string? status)
    {
        var s = (status ?? string.Empty).ToLowerInvariant();
        if (s.Contains("достав"))
            return "success";
        if (s.Contains("отмен"))
            return "danger";
        if (s.Contains("в пути") || s.Contains("назнач"))
            return "primary";
        if (s.Contains("нов") || s.Contains("созда") || s.Contains("ожида"))
            return "soft";
        return "neutral";
    }
}
