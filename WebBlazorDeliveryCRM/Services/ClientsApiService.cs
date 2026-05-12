using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ClientsApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ClientsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<ClientProfileDto?> GetByUserIdAsync(int userId)
    {
        return await _http.GetFromJsonAsync<ClientProfileDto>($"/api/Clients/by-user/{userId}");
    }

    public async Task<ClientProfileDto?> GetProfileAsync(int id)
    {
        return await _http.GetFromJsonAsync<ClientProfileDto>($"/api/Clients/{id}");
    }

    public async Task<List<OrderDto>?> GetOrdersAsync(int clientId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/Clients/{clientId}/orders");
    }

    public async Task<bool> UpdateProfileAsync(int profileId, UpdateClientProfileDto request)
    {
        var response = await _http.PutAsJsonAsync($"/api/Clients/{profileId}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync()
    {
        var list = await _http.GetFromJsonAsync<List<PaymentMethodDto>>("/api/Clients/payment-methods");
        return list ?? new List<PaymentMethodDto>();
    }

    public async Task<BoundCardDto?> GetBoundCardAsync(int profileId)
    {
        return await _http.GetFromJsonAsync<BoundCardDto>($"/api/Clients/{profileId}/bound-card");
    }

    public async Task<(bool ok, string? error)> BindCardAsync(int profileId, BindCardRequestDto request)
    {
        var response = await _http.PostAsJsonAsync($"/api/Clients/{profileId}/bind-card", request);
        if (response.IsSuccessStatusCode)
            return (true, null);
        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return (false, msg.GetString());
                if (doc.RootElement.TryGetProperty("title", out var title) && doc.RootElement.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in errors.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                        {
                            var first = prop.Value[0].GetString();
                            if (!string.IsNullOrWhiteSpace(first))
                                return (false, first);
                        }
                    }
                    return (false, title.GetString());
                }
            }
            catch
            {
            }
        }
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
    }

    public async Task<List<BoundCardListItemDto>> GetBoundCardsAsync(int profileId)
    {
        var stream = await _http.GetStreamAsync($"/api/Clients/{profileId}/bound-cards");
        var list = await JsonSerializer.DeserializeAsync<List<BoundCardListItemDto>>(stream, JsonOpts);
        return list ?? new List<BoundCardListItemDto>();
    }

    public async Task<(bool ok, string? error)> UploadAvatarAsync(int userId, IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(10 * 1024 * 1024);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);
            var response = await _http.PostAsync($"/api/Files/avatar?userId={userId}", content);
            if (response.IsSuccessStatusCode)
                return (true, null);
            var body = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

public sealed class UpdateClientProfileDto
{
    public string? FName { get; set; }
    public string? Name { get; set; }
    public string? Patronumic { get; set; }
    public string? Default_address { get; set; }
    public int? Preferred_payment_method_id { get; set; }
}

public sealed class PaymentMethodDto
{
    public int ID_PaymentMethod { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BindCardRequestDto
{
    public string CardNumber { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public string CardHolder { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
}

public sealed class BoundCardDto
{
    public bool IsBound { get; set; }
    public string? MaskedCard { get; set; }
    public string? Expiry { get; set; }
    public string? CardHolder { get; set; }
}

public sealed class BoundCardListItemDto
{
    public int Id { get; set; }
    public string? MaskedCard { get; set; }
    public string? Expiry { get; set; }
    public string? CardHolder { get; set; }
    public string? PaymentSystem { get; set; }
    public string? SecurityCodeLabel { get; set; }
    public DateTime CreatedAt { get; set; }
}
