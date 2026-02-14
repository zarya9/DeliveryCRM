using System.Net.Http.Json;

namespace WebBlazorDeliveryCRM.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _factory;

    public AuthApiService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient("UnauthorizedClient");
        var response = await client.PostAsJsonAsync("/api/Users/Login", new { email, password });
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
            return null;
        var token = System.Text.Json.JsonSerializer.Deserialize<string>(content);
        return token;
    }
}
