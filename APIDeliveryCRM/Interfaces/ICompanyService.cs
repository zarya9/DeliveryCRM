using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces;

public interface ICompanyService
{
    Task<bool> HasActiveSubscriptionAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Список компаний для выбора при создании заказа. <paramref name="clientMissing"/> — профиль клиента не найден.</summary>
    Task<(IReadOnlyList<CompanyForCustomerOrderDto> List, bool ClientMissing)> GetCompaniesForCustomerOrderAsync(int userId, CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetByIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> UpdateAsync(int companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(int companyId, bool isActive, CancellationToken cancellationToken = default);
}
