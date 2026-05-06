using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public sealed class AccountApiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public AccountApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<(bool ok, string? error)> UpdateMyAccountAsync(UpdateMyAccountRequestDto body)
    {
        var resp = await _http.PutAsJsonAsync("/api/Users/me", body);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        try
        {
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return (false, m.GetString());
        }
        catch
        {
            /* ignore */
        }

        return (false, $"Ошибка {(int)resp.StatusCode}");
    }

    public async Task<UserDto?> GetMyUserAsync(int userId)
    {
        var resp = await _http.GetAsync($"/api/Users/getById?id={userId}");
        if (!resp.IsSuccessStatusCode)
            return null;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<UserDto>(stream, JsonOpts);
    }
}
