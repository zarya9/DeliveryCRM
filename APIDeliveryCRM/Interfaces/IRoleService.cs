using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IRoleService
    {
        Task<IActionResult> GetAllAsync();
    }
}

