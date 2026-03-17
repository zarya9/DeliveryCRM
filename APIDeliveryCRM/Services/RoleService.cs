using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class RoleService : IRoleService
    {
        private readonly ContextDB _context;

        public RoleService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetAllAsync()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new { id = r.ID_Role, name = r.Name })
                .ToListAsync();

            return new OkObjectResult(roles);
        }
    }
}

