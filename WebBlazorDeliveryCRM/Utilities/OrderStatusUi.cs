namespace WebBlazorDeliveryCRM.Utilities;

/// <summary>Тон StatusBadge по названию статуса заказа.</summary>
public static class OrderStatusUi
{
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
