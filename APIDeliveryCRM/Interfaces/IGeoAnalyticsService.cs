using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces;

public interface IGeoAnalyticsService
{
    Task<GeoAnalyticsOverviewDto> GetOverviewAsync(int companyId, DateTime fromUtc, DateTime toUtc, double gridKm);
}
