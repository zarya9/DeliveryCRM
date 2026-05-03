using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var profile = await _clientService.GetByUserIdAsync(userId);
            if (profile == null)
                return new NotFoundResult();
            return new OkObjectResult(profile);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var profile = await _clientService.GetProfileAsync(id);
            if (profile == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(profile);
        }

        [HttpGet("{id:int}/orders")]
        public async Task<IActionResult> GetOrders(int id)
        {
            var orders = await _clientService.GetClientOrdersAsync(id);
            return new OkObjectResult(orders);
        }

        [HttpGet("{id:int}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            return await _clientService.GetClientDetailsAsync(id);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateClientProfileRequest request)
        {
            return await _clientService.UpdateProfileAsync(id, request);
        }

        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            return await _clientService.GetPaymentMethodsAsync();
        }

        [HttpPost("{id:int}/bind-card")]
        public async Task<IActionResult> BindCard(int id, [FromBody] BindClientCardRequest request)
        {
            return await _clientService.BindCardAsync(id, request);
        }

        [HttpGet("{id:int}/bound-card")]
        public async Task<IActionResult> GetBoundCard(int id)
        {
            return await _clientService.GetBoundCardAsync(id);
        }

        [HttpGet("{id:int}/bound-cards")]
        public async Task<IActionResult> GetBoundCards(int id)
        {
            return await _clientService.GetBoundCardsAsync(id);
        }

        [HttpPost("notes")]
        public async Task<IActionResult> AddNote([FromBody] AddClientNoteRequest request)
        {
            return await _clientService.AddClientNoteAsync(request);
        }
    }
}


