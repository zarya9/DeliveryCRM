using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Состояние страницы маршрутов логиста в рамках одной сессии Blazor (переходы по меню не сбрасывают список точек и выбранного курьера).
/// </summary>
public sealed class LogisticianRoutePlanningSession
{
    public List<RoutePlanningPointDto> RoutePoints { get; } = new();

    public double FuelPer100Km { get; set; } = 10.0;

    public int RouteAssignCourierId { get; set; }

    public string? RouteMessage { get; set; }

    public double? MapCenterLat { get; set; }
    public double? MapCenterLon { get; set; }
    public int? MapZoom { get; set; }

}
