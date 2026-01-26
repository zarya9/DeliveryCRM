using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Interfaces
{
    public interface IReviewService
    {
        Task<IActionResult> AddReviewAsync(AddReviewRequest dto);
        Task<IActionResult> EditReviewAsync(int reviewId, EditReviewRequest dto);
        Task<IActionResult> GetReviewsByUserAsync(int targetUserId);
        Task<IActionResult> GetReviewsByOrderAsync(int orderId);
    }
}


