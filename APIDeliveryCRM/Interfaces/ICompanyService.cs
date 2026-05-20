using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces;

public interface ICompanyService
{
    Task<bool> HasActiveSubscriptionAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Список компаний для выбора при создании заказа. <paramref name="clientMissing"/> — профиль клиента не найден.</summary>
    Task<(IReadOnlyList<CompanyForCustomerOrderDto> List, bool ClientMissing)> GetCompaniesForCustomerOrderAsync(int userId, CancellationToken cancellationToken = default);
}
