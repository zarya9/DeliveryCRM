using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public Task<IActionResult> GetByCompany([FromQuery] int companyId)
        {
            return _employeeService.GetByCompanyAsync(companyId);
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, [FromQuery] int companyId)
        {
            return _employeeService.CreateAsync(request, companyId);
        }
    }
}

