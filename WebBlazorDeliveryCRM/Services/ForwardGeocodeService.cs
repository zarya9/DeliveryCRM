using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebBlazorDeliveryCRM.Services;

/// <summary>
/// Прямое геокодирование (OpenStreetMap Nominatim) на сервере Blazor — для карты, если в заказе нет координат.
/// </summary>
public sealed class ForwardGeocodeService
{
    private readonly HttpClient _http;

    public ForwardGeocodeService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(decimal? lat, decimal? lon)> TryGeocodeAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (null, null);

        var url =
            "https://nominatim.openstreetmap.org/search?format=json&limit=1&addressdetails=0&q=" +
            Uri.EscapeDataString(query.Trim());

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.Clear();
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("DeliveryCRM-Blazor", "1.0"));

        using var resp = await _http.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return (null, null);

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return (null, null);

        var first = doc.RootElement[0];
        if (!first.TryGetProperty("lat", out var latEl) || !first.TryGetProperty("lon", out var lonEl))
            return (null, null);

        var latRaw = latEl.ValueKind == JsonValueKind.String ? latEl.GetString() : latEl.GetRawText();
        var lonRaw = lonEl.ValueKind == JsonValueKind.String ? lonEl.GetString() : lonEl.GetRawText();
        if (string.IsNullOrWhiteSpace(latRaw) || string.IsNullOrWhiteSpace(lonRaw))
            return (null, null);

        if (!decimal.TryParse(latRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ||
            !decimal.TryParse(lonRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lo))
            return (null, null);

        return (la, lo);
    }
}
