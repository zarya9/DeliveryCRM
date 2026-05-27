namespace APIDeliveryCRM.Utilities;

public static class OrderStatusRules
{
    public static bool IsCancelled(string? statusName)
    {
        var s = (statusName ?? string.Empty).Trim();
        if (s.Length == 0)
            return false;
        return s.Contains("отмен", StringComparison.OrdinalIgnoreCase)
               || s.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDelivered(string? statusName, DateTime? deliveredAt)
    {
        if (deliveredAt.HasValue)
            return true;
        var s = (statusName ?? string.Empty).Trim();
        return s.Contains("доставлен", StringComparison.OrdinalIgnoreCase)
               || s.Equals("delivered", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Заказы, которые клиент видит в «активных» (не завершённые и не отменённые).</summary>
    public static bool IsActiveForClient(string? statusName, DateTime? deliveredAt)
        => !IsCancelled(statusName) && !IsDelivered(statusName, deliveredAt);
}
