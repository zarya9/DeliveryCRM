using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Interfaces;

public interface ILogisticsHubService
{
    Task<IReadOnlyList<LogisticsHub>> GetByCompanyAsync(int companyId);
    Task<LogisticsHub> CreateAsync(int companyId, int userId, CreateLogisticsHubRequest request);
}
