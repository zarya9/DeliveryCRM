using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IClientService
    {
        Task<ClientProfile> GetProfileAsync(int clientProfileId);
        Task<ClientProfile> GetByUserIdAsync(int userId);
        Task<IReadOnlyList<Order>> GetClientOrdersAsync(int clientProfileId, bool activeOnly = false);
        Task<IActionResult> UpdateProfileAsync(int clientProfileId, UpdateClientProfileRequest request);
        Task<IActionResult> GetPaymentMethodsAsync();
        Task<IActionResult> BindCardAsync(int clientProfileId, BindClientCardRequest request);
        Task<IActionResult> GetBoundCardAsync(int clientProfileId);
        Task<IActionResult> GetBoundCardsAsync(int clientProfileId);
        Task<IActionResult> DeleteBoundCardAsync(int clientProfileId, int cardNoteId, int? actorUserId = null);
        Task<IActionResult> GetClientDetailsAsync(int clientProfileId);
        Task<IActionResult> AddClientNoteAsync(AddClientNoteRequest request);
        Task<IActionResult> GetChatContactUserIdAsync(int clientUserId, int? orderId = null);
    }
}


