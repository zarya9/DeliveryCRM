using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressSuggestController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AddressSuggestController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Suggest([FromQuery] string q, [FromQuery] int limit = 7)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            return Ok(new List<object>());
        limit = Math.Clamp(limit, 1, 10);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryCRM/1.0 (address-suggest)");
        var url = $"https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&limit={limit}&countrycodes=ru&q={Uri.EscapeDataString(q.Trim())}";
        var resp = await client.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return Ok(new List<object>());

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var rows = new List<object>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var display = item.TryGetProperty("display_name", out var d) ? d.GetString() : null;
            var lat = item.TryGetProperty("lat", out var latEl) ? latEl.GetString() : null;
            var lon = item.TryGetProperty("lon", out var lonEl) ? lonEl.GetString() : null;
            string? road = null;
            string? city = null;
            string? house = null;
            string? state = null;
            if (item.TryGetProperty("address", out var address))
            {
                if (address.TryGetProperty("road", out var roadEl)) road = roadEl.GetString();
                if (address.TryGetProperty("house_number", out var houseEl)) house = houseEl.GetString();
                if (address.TryGetProperty("city", out var cityEl)) city = cityEl.GetString();
                if (string.IsNullOrWhiteSpace(city) && address.TryGetProperty("town", out var townEl)) city = townEl.GetString();
                if (string.IsNullOrWhiteSpace(city) && address.TryGetProperty("village", out var villageEl)) city = villageEl.GetString();
                if (address.TryGetProperty("state", out var stateEl)) state = stateEl.GetString();
                if (string.IsNullOrWhiteSpace(state) && address.TryGetProperty("region", out var regionEl)) state = regionEl.GetString();
            }

            var primaryLine = !string.IsNullOrWhiteSpace(road)
                ? (string.IsNullOrWhiteSpace(house) ? road : $"{road}, {house}")
                : (display?.Split(',').FirstOrDefault()?.Trim());
            var secondaryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(city)) secondaryParts.Add(city!);
            if (!string.IsNullOrWhiteSpace(state)) secondaryParts.Add(state!);
            var secondaryLine = string.Join(", ", secondaryParts);

            rows.Add(new
            {
                displayName = display,
                street = road,
                house,
                city,
                state,
                primaryLine,
                secondaryLine,
                hasHouse = !string.IsNullOrWhiteSpace(house),
                lat,
                lon
            });
        }

        return Ok(rows);
    }
}
