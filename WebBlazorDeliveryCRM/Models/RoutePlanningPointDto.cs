namespace WebBlazorDeliveryCRM.Models;

/// <summary>Точка маршрута на странице логиста (карта + список адресов).</summary>
public sealed class RoutePlanningPointDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Address { get; set; } = string.Empty;
    public string Source { get; set; } = "manual";
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    /// <summary>Заказ при импорте из заказов; для массового назначения курьера.</summary>
    public int? RelatedOrderId { get; set; }
}
