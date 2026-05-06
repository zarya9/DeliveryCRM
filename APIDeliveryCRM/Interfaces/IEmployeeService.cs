using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IEmployeeService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId);
        Task<IActionResult> CreateAsync(CreateEmployeeRequest request, int companyId);
        Task<IActionResult> FireAsync(int employeeId, int companyId, int actorUserId);
        Task<IActionResult> ChangeRoleAsync(int employeeId, int companyId, int actorUserId, int roleId);
    }
}

