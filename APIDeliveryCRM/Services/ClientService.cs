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
                .Include(c => c.ClientStatus)
                .Include(c => c.ClientSegment)
                .Include(c => c.PaymentMethod)
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientProfileId);
        }

        public async Task<ClientProfile> GetByUserIdAsync(int userId)
        {
            return await _context.ClientProfiles
                .Include(c => c.User)
                .Include(c => c.ClientStatus)
                .Include(c => c.ClientSegment)
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

        public async Task<IActionResult> GetClientDetailsAsync(int clientProfileId)
        {
            var client = await _context.ClientProfiles
                .Include(c => c.User)
                    .ThenInclude(u => u.Logins)
                .Include(c => c.ClientStatus)
                .Include(c => c.ClientSegment)
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientProfileId);

            if (client == null)
            {
                return new NotFoundObjectResult(new { message = "Клиент не найден" });
            }

            var email = client.User.Logins
                .OrderBy(l => l.ID_Login)
                .Select(l => l.Email)
                .FirstOrDefault() ?? string.Empty;

            var orders = await _context.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.Client_id == clientProfileId)
                .OrderByDescending(o => o.Created_at)
                .Take(100)
                .Select(o => new ClientOrderShortDto
                {
                    OrderId = o.ID_Order,
                    OrderNumber = o.Order_Number,
                    Name = o.Name,
                    Status = o.OrderStatus.Name,
                    CreatedAt = o.Created_at,
                    DeliveredAt = o.Delivered_at,
                    EstimatedCost = o.Estimated_cost,
                    FinalCost = o.Final_cost
                })
                .ToListAsync();

            var notes = await _context.ClientNotes
                .Where(n => n.ClientProfile_id == clientProfileId)
                .Include(n => n.Author)
                .Include(n => n.ClientNoteType)
                .OrderByDescending(n => n.Created_at)
                .Take(50)
                .Select(n => new ClientNoteShortDto
                {
                    Id = n.ID_ClientNote,
                    Type = n.ClientNoteType.Name,
                    Text = n.Text,
                    CreatedAt = n.Created_at,
                    AuthorName = n.Author.FName + " " + n.Author.Name
                })
                .ToListAsync();

            var dto = new ClientDetailsResponse
            {
                ClientProfileId = client.ID_ClientProfile,
                UserId = client.User_id,
                FName = client.User.FName,
                Name = client.User.Name,
                Patronumic = client.User.Patronumic,
                Email = email,
                Phone = null,
                Rating = client.Rating,
                Status = client.ClientStatus?.Name,
                Segment = client.ClientSegment?.Name,
                Orders = orders,
                Notes = notes
            };

            return new OkObjectResult(dto);
        }

        public async Task<IActionResult> AddClientNoteAsync(AddClientNoteRequest request)
        {
            var client = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == request.ClientProfileId);
            if (client == null)
            {
                return new NotFoundObjectResult(new { message = "Клиент не найден" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID_User == request.AuthorUserId);
            if (user == null)
            {
                return new BadRequestObjectResult(new { message = "Автор не найден" });
            }

            var note = new ClientNote
            {
                ClientProfile_id = request.ClientProfileId,
                Author_id = request.AuthorUserId,
                Text = request.Text.Trim()
            };

            var typeCode = string.IsNullOrWhiteSpace(request.Type) ? "NOTE" : request.Type.Trim();
            var noteType = await _context.ClientNoteTypes
                .FirstOrDefaultAsync(t => t.Code == typeCode);
            if (noteType == null)
            {
                return new BadRequestObjectResult(new { message = "Некорректный тип заметки" });
            }

            note.ClientNoteType_id = noteType.ID_ClientNoteType;

            _context.ClientNotes.Add(note);
            await _context.SaveChangesAsync();
            return new OkObjectResult(note);
        }
    }
}


