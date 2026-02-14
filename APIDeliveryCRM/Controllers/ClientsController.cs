using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateClientProfileRequest request)
        {
            return await _clientService.UpdateProfileAsync(id, request);
        }
    }
}


