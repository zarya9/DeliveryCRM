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
    public class ReviewService : IReviewService
    {
        private readonly ContextDB _context;

        public ReviewService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> AddReviewAsync(AddReviewRequest dto)
        {
            var review = new Review
            {
                Order_id = dto.OrderId,
                Author_id = dto.AuthorId,
                TargetUser_id = dto.TargetUserId,
                Rating = dto.Rating,
                Title = dto.Title,
                Message = dto.Message
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return new OkObjectResult(review);
        }

        public async Task<IActionResult> EditReviewAsync(int reviewId, EditReviewRequest dto)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ID_Review == reviewId);
            if (review == null)
            {
                return new NotFoundResult();
            }

            review.Rating = dto.Rating;
            review.Title = dto.Title;
            review.Message = dto.Message;

            await _context.SaveChangesAsync();
            return new OkObjectResult(review);
        }

        public async Task<IActionResult> GetReviewsByUserAsync(int targetUserId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.TargetUser_id == targetUserId)
                .Include(r => r.Order)
                .Include(r => r.UserAuthor)
                .ToListAsync();

            return new OkObjectResult(reviews);
        }

        public async Task<IActionResult> GetReviewsByOrderAsync(int orderId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.Order_id == orderId)
                .Include(r => r.UserAuthor)
                .Include(r => r.UserTarget)
                .ToListAsync();

            return new OkObjectResult(reviews);
        }
    }
}


