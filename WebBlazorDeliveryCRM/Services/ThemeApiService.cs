using System.Net.Http.Json;

namespace WebBlazorDeliveryCRM.Services;

public class ThemeApiService
{
    private readonly HttpClient _http;

    public ThemeApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<string?> GetThemeAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ThemeResponse>($"/api/Theme/{userId}", cancellationToken);
            return resp?.Theme;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SetThemeAsync(int userId, string theme, CancellationToken cancellationToken = default)
    {
        var body = new { themeCode = theme };
        var resp = await _http.PostAsJsonAsync($"/api/Theme/{userId}", body, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    private sealed class ThemeResponse
    {
        public string Theme { get; set; } = "light";
    }
}

