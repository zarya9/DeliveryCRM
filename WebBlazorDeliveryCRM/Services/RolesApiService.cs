using System.Net.Http.Json;

namespace WebBlazorDeliveryCRM.Services;

public class RolesApiService
{
    private readonly HttpClient _http;

    public RolesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync("/api/Roles", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new List<RoleDto>();
            }

            var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(cancellationToken: cancellationToken);
            return roles ?? new List<RoleDto>();
        }
        catch
        {
            return new List<RoleDto>();
        }
    }

    public sealed class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}

