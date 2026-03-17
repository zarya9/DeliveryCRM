using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IEmployeeService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId);
        Task<IActionResult> CreateAsync(CreateEmployeeRequest request, int companyId);
    }
}

