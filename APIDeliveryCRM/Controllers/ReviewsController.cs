using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public Task<IActionResult> Add([FromBody] AddReviewRequest dto) => _reviewService.AddReviewAsync(dto);

        [HttpGet("user/{userId:int}")]
        public Task<IActionResult> GetForUser(int userId) => _reviewService.GetReviewsByUserAsync(userId);

        [HttpGet("order/{orderId:int}")]
        public Task<IActionResult> GetForOrder(int orderId) => _reviewService.GetReviewsByOrderAsync(orderId);

        [HttpPut("{reviewId:int}")]
        public Task<IActionResult> Edit(int reviewId, [FromBody] EditReviewRequest dto) =>
            _reviewService.EditReviewAsync(reviewId, dto);
    }
}


