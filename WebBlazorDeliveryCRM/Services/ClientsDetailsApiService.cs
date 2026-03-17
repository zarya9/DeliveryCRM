using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services
{
    public class ClientsDetailsApiService
    {
        private readonly HttpClient _http;

        public ClientsDetailsApiService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AuthorizedClient");
        }

        public async Task<ClientDetailsDto?> GetDetailsAsync(int clientProfileId)
        {
            var url = $"/api/Clients/{clientProfileId}/details";
            var dto = await _http.GetFromJsonAsync<ClientDetailsDto>(url);
            return dto;
        }

        public async Task<bool> AddNoteAsync(int clientProfileId, int authorUserId, string typeCode, string text)
        {
            var payload = new
            {
                ClientProfileId = clientProfileId,
                AuthorUserId = authorUserId,
                Type = typeCode,
                Text = text
            };

            var response = await _http.PostAsJsonAsync("/api/Clients/notes", payload);
            return response.IsSuccessStatusCode;
        }
    }
}

