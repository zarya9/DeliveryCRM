using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class VehiclesApiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public VehiclesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<VehicleFormLookupsDto?> GetLookupsAsync()
    {
        var resp = await _http.GetAsync("/api/Vehicles/lookups");
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<VehicleFormLookupsDto>();
    }

    public async Task<List<IdNameDto>> GetCatalogBrandsAsync()
    {
        var resp = await _http.GetAsync("/api/Vehicles/catalog/brands");
        if (!resp.IsSuccessStatusCode)
            return new List<IdNameDto>();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var list = await JsonSerializer.DeserializeAsync<List<IdNameDto>>(stream, JsonOpts);
        return list ?? new List<IdNameDto>();
    }

    public async Task<List<VehicleCatalogModelDto>> GetCatalogModelsAsync(int? brandId = null)
    {
        var url = brandId is > 0
            ? $"/api/Vehicles/catalog/models?brandId={brandId.Value}"
            : "/api/Vehicles/catalog/models";
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return new List<VehicleCatalogModelDto>();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var list = await JsonSerializer.DeserializeAsync<List<VehicleCatalogModelDto>>(stream, JsonOpts);
        return list ?? new List<VehicleCatalogModelDto>();
    }

    public async Task<(bool ok, string? error, int? id)> CreateVehicleAsync(CreateVehicleApiRequest body)
    {
        var json = JsonSerializer.Serialize(body);
        var resp = await _http.PostAsync("/api/Vehicles", new StringContent(json, Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
        {
            var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id))
                return (true, null, id);
            return (true, null, null);
        }

        var err = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(err))
        {
            try
            {
                using var doc = JsonDocument.Parse(err);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    return (false, m.GetString(), null);
                if (doc.RootElement.TryGetProperty("title", out var t) && !string.IsNullOrWhiteSpace(t.GetString()))
                    return (false, t.GetString(), null);
                if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var first = errors.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                            ? p.Value.EnumerateArray().Select(v => v.GetString())
                            : new[] { p.Value.GetString() })
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    if (!string.IsNullOrWhiteSpace(first))
                        return (false, first, null);
                }
            }
            catch
            {
            }
        }
        return (false, string.IsNullOrWhiteSpace(err) ? $"HTTP {(int)resp.StatusCode}" : err, null);
    }

    public async Task<List<VehicleExpiringDocDto>> GetExpiringDocsAsync(int days = 14)
    {
        var resp = await _http.GetAsync($"/api/Vehicles/expiring-docs?days={days}");
        if (!resp.IsSuccessStatusCode)
            return new List<VehicleExpiringDocDto>();
        var items = await resp.Content.ReadFromJsonAsync<List<VehicleExpiringDocDto>>();
        return items ?? new List<VehicleExpiringDocDto>();
    }
}

public class VehicleExpiringDocDto
{
    public int Id { get; set; }
    public string? Plate { get; set; }
    public DateTime? InsuranceExpiresAt { get; set; }
    public DateTime? RegistrationExpiresAt { get; set; }
    public DateTime? MaintenanceDueAt { get; set; }
    public bool IsAvailable { get; set; }
}
