using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
            var profile = await _context.ClientProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ID_ClientProfile == clientProfileId);
            if (profile == null)
            {
                return new NotFoundObjectResult(new { message = "Профиль клиента не найден" });
            }

            if (!string.IsNullOrWhiteSpace(request.FName))
            {
                profile.User.FName = request.FName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                profile.User.Name = request.Name.Trim();
            }

            if (request.Patronumic != null)
            {
                profile.User.Patronumic = string.IsNullOrWhiteSpace(request.Patronumic) ? null : request.Patronumic.Trim();
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

        public async Task<IActionResult> GetPaymentMethodsAsync()
        {
            var methods = await _context.PaymentMethods
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.ID_PaymentMethod, x.Name })
                .ToListAsync();

            return new OkObjectResult(methods);
        }

        public async Task<IActionResult> BindCardAsync(int clientProfileId, BindClientCardRequest request)
        {
            var profile = await _context.ClientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientProfileId);
            if (profile == null)
                return new NotFoundObjectResult(new { message = "Профиль клиента не найден" });

            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID_User == profile.User_id);
            if (author == null)
                return new BadRequestObjectResult(new { message = "Не найден пользователь клиента." });

            var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length != 16)
                return new BadRequestObjectResult(new { message = "Номер карты должен содержать 16 цифр." });

            var masked = $"**** **** **** {digits[^4..]}";
            var expiry = (request.Expiry ?? string.Empty).Trim();
            var holder = (request.CardHolder ?? string.Empty).Trim();
            var cvv = (request.Cvv ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(expiry) || string.IsNullOrWhiteSpace(holder) || string.IsNullOrWhiteSpace(cvv))
                return new BadRequestObjectResult(new { message = "Укажите срок действия, имя владельца и CVV." });
            if (!Regex.IsMatch(expiry, "^(0[1-9]|1[0-2])\\/\\d{2}$"))
                return new BadRequestObjectResult(new { message = "Срок действия укажите в формате MM/YY." });
            var paymentSystem = DetectPaymentSystem(digits);
            var securityLabel = GetSecurityCodeLabel(paymentSystem);
            var expectedLength = paymentSystem == "UnionPay" ? 3 : 3;
            if (!Regex.IsMatch(cvv, "^\\d{3,4}$"))
                return new BadRequestObjectResult(new { message = $"{securityLabel} должен содержать только цифры." });
            if (cvv.Length != expectedLength)
                return new BadRequestObjectResult(new { message = $"{securityLabel} для {paymentSystem} должен содержать {expectedLength} цифры." });

            var noteType = await EnsureCardNoteTypeAsync();
            var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            // Не храним PAN/CVV, только маску + метаданные.
            var payload = $"CARDV3|{masked}|{expiry}|{holder}|{paymentSystem}|{securityLabel}|{token}";

            var cardNote = new ClientNote
            {
                ClientProfile_id = clientProfileId,
                Author_id = author.ID_User,
                ClientNoteType_id = noteType.ID_ClientNoteType,
                Text = payload,
                Created_at = DateTime.UtcNow
            };
            _context.ClientNotes.Add(cardNote);

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Карта успешно привязана", maskedCard = masked, expiry, cardHolder = holder });
        }

        public async Task<IActionResult> GetBoundCardAsync(int clientProfileId)
        {
            var noteType = await _context.ClientNoteTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == "CARD_BINDING");
            if (noteType == null)
                return new OkObjectResult(new { isBound = false });

            var note = await _context.ClientNotes
                .AsNoTracking()
                .Where(n => n.ClientProfile_id == clientProfileId && n.ClientNoteType_id == noteType.ID_ClientNoteType)
                .OrderByDescending(n => n.Created_at)
                .FirstOrDefaultAsync();
            if (note == null || string.IsNullOrWhiteSpace(note.Text))
                return new OkObjectResult(new { isBound = false });

            var parts = note.Text.Split('|');
            if (parts.Length >= 7 && string.Equals(parts[0], "CARDV3", StringComparison.OrdinalIgnoreCase))
            {
                return new OkObjectResult(new
                {
                    isBound = true,
                    maskedCard = parts[1],
                    expiry = parts[2],
                    cardHolder = parts[3]
                });
            }

            if (parts.Length >= 5 && string.Equals(parts[0], "CARDV2", StringComparison.OrdinalIgnoreCase))
            {
                return new OkObjectResult(new
                {
                    isBound = true,
                    maskedCard = parts[1],
                    expiry = parts[2],
                    cardHolder = parts[3]
                });
            }

            if (parts.Length < 4 || !string.Equals(parts[0], "CARD", StringComparison.OrdinalIgnoreCase))
                return new OkObjectResult(new { isBound = false });

            return new OkObjectResult(new
            {
                isBound = true,
                maskedCard = parts[1],
                expiry = parts[2],
                cardHolder = parts[3]
            });
        }

        public async Task<IActionResult> GetBoundCardsAsync(int clientProfileId)
        {
            var noteType = await _context.ClientNoteTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == "CARD_BINDING");
            if (noteType == null)
                return new OkObjectResult(new List<object>());

            var notes = await _context.ClientNotes
                .AsNoTracking()
                .Where(n => n.ClientProfile_id == clientProfileId && n.ClientNoteType_id == noteType.ID_ClientNoteType)
                .OrderByDescending(n => n.Created_at)
                .ToListAsync();

            var rows = notes
                .Select(n =>
                {
                    if (string.IsNullOrWhiteSpace(n.Text))
                        return null;
                    var parts = n.Text.Split('|');
                    if (parts.Length >= 7 && string.Equals(parts[0], "CARDV3", StringComparison.OrdinalIgnoreCase))
                        return new { id = n.ID_ClientNote, maskedCard = parts[1], expiry = parts[2], cardHolder = parts[3], paymentSystem = parts[4], securityCodeLabel = parts[5], createdAt = n.Created_at };
                    if (parts.Length >= 5 && string.Equals(parts[0], "CARDV2", StringComparison.OrdinalIgnoreCase))
                        return new { id = n.ID_ClientNote, maskedCard = parts[1], expiry = parts[2], cardHolder = parts[3], paymentSystem = DetectPaymentSystemByMask(parts[1]), securityCodeLabel = "CVV", createdAt = n.Created_at };
                    if (parts.Length >= 4 && string.Equals(parts[0], "CARD", StringComparison.OrdinalIgnoreCase))
                        return new { id = n.ID_ClientNote, maskedCard = parts[1], expiry = parts[2], cardHolder = parts[3], paymentSystem = "Неизвестно", securityCodeLabel = "CVV", createdAt = n.Created_at };
                    return null;
                })
                .Where(x => x != null)
                .ToList();

            return new OkObjectResult(rows);
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

        private async Task<ClientNoteType> EnsureCardNoteTypeAsync()
        {
            var noteType = await _context.ClientNoteTypes
                .FirstOrDefaultAsync(t => t.Code == "CARD_BINDING");
            if (noteType != null)
                return noteType;

            noteType = new ClientNoteType
            {
                Name = "Привязка карты",
                Code = "CARD_BINDING"
            };
            _context.ClientNoteTypes.Add(noteType);
            await _context.SaveChangesAsync();
            return noteType;
        }

        private static string DetectPaymentSystem(string digits)
        {
            if (string.IsNullOrWhiteSpace(digits))
                return "Неизвестно";
            if (digits.StartsWith("4"))
                return "Visa";
            if (digits.StartsWith("22"))
                return "Мир";
            if (digits.StartsWith("5"))
                return "Mastercard";
            if (digits.StartsWith("6"))
                return "UnionPay";
            return "Неизвестно";
        }

        private static string DetectPaymentSystemByMask(string maskedCard)
        {
            // По маске определить нельзя, возвращаем безопасный fallback.
            return "Неизвестно";
        }

        private static string GetSecurityCodeLabel(string paymentSystem) => paymentSystem switch
        {
            "Mastercard" => "CVC",
            "Visa" => "CVV",
            "Мир" => "CVP",
            "UnionPay" => "CVN",
            _ => "CVV"
        };
    }
}


