using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class ClientService : IClientService
    {
        private readonly ContextDB _context;

        public ClientService(ContextDB context)
        {
            _context = context;
        }

        public async Task<ClientProfile> GetProfileAsync(int clientProfileId)
        {
            return await _context.ClientProfiles
                .Include(c => c.User)
                .Include(c => c.PaymentMethod)
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientProfileId);
        }

        public async Task<ClientProfile> GetByUserIdAsync(int userId)
        {
            return await _context.ClientProfiles
                .Include(c => c.User)
                .Include(c => c.PaymentMethod)
                .FirstOrDefaultAsync(c => c.User_id == userId);
        }

        public async Task<IReadOnlyList<Order>> GetClientOrdersAsync(int clientProfileId)
        {
            return await _context.Orders
                .Where(o => o.Client_id == clientProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.CourierProfile)
                .ToListAsync();
        }

        public async Task<IActionResult> UpdateProfileAsync(int clientProfileId, UpdateClientProfileRequest request)
        {
            var profile = await _context.ClientProfiles.FindAsync(clientProfileId);
            if (profile == null)
            {
                return new NotFoundObjectResult(new { message = "Профиль клиента не найден" });
            }

            if (!string.IsNullOrEmpty(request.Default_address))
            {
                profile.Default_address = request.Default_address;
            }

            if (request.Preferred_payment_method_id.HasValue)
            {
                var paymentMethod = await _context.PaymentMethods.FindAsync(request.Preferred_payment_method_id.Value);
                if (paymentMethod == null)
                {
                    return new BadRequestObjectResult(new { message = "Способ оплаты не найден" });
                }
                profile.Preferred_payment_method_id = request.Preferred_payment_method_id.Value;
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Профиль успешно обновлен", profile });
        }
    }
}


