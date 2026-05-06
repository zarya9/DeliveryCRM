using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    public async Task<IActionResult> Suggest([FromQuery] string q, [FromQuery] string? city = null, [FromQuery] int limit = 7)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            return Ok(new List<object>());
        limit = Math.Clamp(limit, 1, 10);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryCRM/1.0 (address-suggest)");
        client.Timeout = TimeSpan.FromSeconds(12);

        var rows = await FetchNominatimAsync(client, q.Trim(), string.IsNullOrWhiteSpace(city) ? null : city.Trim(), limit);

        return Ok(rows);
    }

    private static async Task<List<object>> FetchNominatimAsync(HttpClient client, string q, string? city, int limit)
    {
        SplitStreetAndHouse(q, out var streetQuery, out var houseFromQuery);

        var list = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSemantic = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var requests = 0;

        bool TryAdd(JsonElement item)
        {
            if (list.Count >= limit)
                return false;
            if (!TryReadAddressFields(item, out var road, out var house, out var resCity, out var state, out var display))
                return false;

            if (!IsRelevantToUserQuery(q, road, display))
                return false;

            if (!string.IsNullOrWhiteSpace(city))
            {
                var inStructuredCity = !string.IsNullOrWhiteSpace(resCity) && CityMatches(city, resCity);
                var inDisplay = display != null && display.Contains(city, StringComparison.OrdinalIgnoreCase);
                if (!inStructuredCity && !inDisplay)
                    return false;
            }

            var key = GetDedupeKey(item, display);
            if (string.IsNullOrEmpty(key) || seen.Contains(key))
                return false;

            var row = MapItem(road, house, resCity, state, display, item);
            if (row is null)
                return false;

            var semantic = SemanticDedupeKey(road, house, resCity, state, display);
            if (!string.IsNullOrEmpty(semantic) && seenSemantic.Contains(semantic))
                return false;

            seen.Add(key);
            if (!string.IsNullOrEmpty(semantic))
                seenSemantic.Add(semantic);
            list.Add(row);
            return true;
        }

        async Task CollectAsync(string url)
        {
            requests++;
            using var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return;
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (list.Count >= limit)
                    return;
                TryAdd(item);
            }
        }

        // 1) РЎС‚СЂСѓРєС‚СѓСЂРёСЂРѕРІР°РЅРЅС‹Р№: СѓР»РёС†Р° + РіРѕСЂРѕРґ (+ РЅРѕРјРµСЂ, РµСЃР»Рё СЂР°СЃРїРѕР·РЅР°Р»Рё).
        if (!string.IsNullOrWhiteSpace(city))
        {
            var url =
                $"https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&dedupe=1&limit={limit}" +
                $"&countrycodes=ru&street={Uri.EscapeDataString(streetQuery)}&city={Uri.EscapeDataString(city)}";
            if (!string.IsNullOrWhiteSpace(houseFromQuery))
                url += $"&housenumber={Uri.EscapeDataString(houseFromQuery)}";
            await CollectAsync(url);
        }

        // 2) Р”РѕРїРѕР»РЅСЏРµРј СЃРІРѕР±РѕРґРЅС‹Рј РїРѕРёСЃРєРѕРј (РґСЂСѓРіР°СЏ СЂР°РЅР¶РёСЂРѕРІРєР°; РїРѕСЃР»Рµ СЃС‚СЂСѓРєС‚СѓСЂРёСЂРѕРІР°РЅРЅРѕРіРѕ вЂ” РїР°СѓР·Р° РґР»СЏ Nominatim).
        if (list.Count < limit)
        {
            if (requests > 0)
                await Task.Delay(1100);

            var freeQ = string.IsNullOrWhiteSpace(city) ? q : $"{city}, {streetQuery}";
            var free =
                $"https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&dedupe=1&limit={limit}" +
                $"&countrycodes=ru&q={Uri.EscapeDataString(freeQ)}";
            await CollectAsync(free);
        }

        // 3) Photon вЂ” С‡Р°С‰Рµ РґР°С‘С‚ house_number РїРѕ СѓР»РёС†Рµ (OSM РІ Р Р¤ С‡Р°СЃС‚Рѕ Р±РµР· РґРѕРјРѕРІ РІ Nominatim).
        if (list.Count < limit && !string.IsNullOrWhiteSpace(city))
        {
            if (requests > 0)
                await Task.Delay(1100);
            await AppendPhotonAsync(client, city, streetQuery, limit, list, seen, seenSemantic, q);
        }

        return list;
    }

    private static string SemanticDedupeKey(string? road, string? house, string? resCity, string? state, string? display)
    {
        var r = (road ?? "").Trim().ToLowerInvariant();
        var h = (house ?? "").Trim().ToLowerInvariant();
        var c = (resCity ?? "").Trim().ToLowerInvariant();
        var s = (state ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(r) && !string.IsNullOrEmpty(display))
            r = display.Split(',').FirstOrDefault()?.Trim().ToLowerInvariant() ?? "";
        return $"{r}|{h}|{c}|{s}";
    }

    private static async Task AppendPhotonAsync(
        HttpClient client,
        string city,
        string streetQuery,
        int limit,
        List<object> list,
        HashSet<string> seen,
        HashSet<string> seenSemantic,
        string userQuery)
    {
        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryCRM/1.0 (address-suggest-photon)");
            var pq = $"{city} {streetQuery}".Trim();
            var url = "https://photon.komoot.io/api/?lang=ru&limit=25&q=" + Uri.EscapeDataString(pq);
            using var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return;
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("features", out var feats))
                return;
            foreach (var f in feats.EnumerateArray())
            {
                if (list.Count >= limit)
                    return;
                if (!f.TryGetProperty("properties", out var p))
                    continue;
                var road = p.TryGetProperty("street", out var st) ? st.GetString() : null;
                if (string.IsNullOrWhiteSpace(road) && p.TryGetProperty("name", out var nm))
                    road = nm.GetString();
                var house = p.TryGetProperty("housenumber", out var hn) ? hn.GetString() : null;
                var resCity = p.TryGetProperty("city", out var pc) ? pc.GetString() : null;
                if (string.IsNullOrWhiteSpace(resCity) && p.TryGetProperty("town", out var pt))
                    resCity = pt.GetString();
                var state = p.TryGetProperty("state", out var ps) ? ps.GetString() : null;
                string? display = null;
                if (!string.IsNullOrWhiteSpace(road) && !string.IsNullOrWhiteSpace(resCity))
                    display = string.IsNullOrWhiteSpace(house) ? $"{road}, {resCity}" : $"{road}, {house}, {resCity}";
                else if (p.TryGetProperty("name", out var pn))
                    display = pn.GetString();
                if (string.IsNullOrWhiteSpace(display))
                    continue;

                if (!IsRelevantToUserQuery(userQuery, road, display))
                    continue;

                var inCity = !string.IsNullOrWhiteSpace(resCity) && CityMatches(city, resCity);
                var inDisp = display.Contains(city, StringComparison.OrdinalIgnoreCase);
                if (!inCity && !inDisp)
                    continue;

                string? latStr = null;
                string? lonStr = null;
                if (f.TryGetProperty("geometry", out var geom) &&
                    geom.TryGetProperty("coordinates", out var coords) &&
                    coords.ValueKind == JsonValueKind.Array && coords.GetArrayLength() >= 2)
                {
                    lonStr = coords[0].GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                    latStr = coords[1].GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var semantic = SemanticDedupeKey(road, house, resCity, state, display);
                if (!string.IsNullOrEmpty(semantic) && seenSemantic.Contains(semantic))
                    continue;

                var osmPart = p.TryGetProperty("osm_id", out var oid) && oid.ValueKind == JsonValueKind.Number
                    ? oid.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : Guid.NewGuid().ToString("N");
                var fakeKey = "ph:" + osmPart;
                if (seen.Contains(fakeKey))
                    continue;

                var row = MapItemCore(road, house, resCity, state, display, latStr, lonStr);
                if (row is null)
                    continue;

                seen.Add(fakeKey);
                if (!string.IsNullOrEmpty(semantic))
                    seenSemantic.Add(semantic);
                list.Add(row);
            }
        }
        catch
        {
            /* Photon РЅРµРґРѕСЃС‚СѓРїРµРЅ вЂ” РѕСЃС‚Р°С‘РјСЃСЏ РЅР° Nominatim */
        }
    }

    private static void SplitStreetAndHouse(string q, out string streetPart, out string? housePart)
    {
        streetPart = q.Trim();
        housePart = null;
        var m = Regex.Match(
            q.Trim(),
            @"^(?<s>.+?)[\s,]+(?<h>\d+[Рђ-РЇР°-СЏA-Za-z/-]*)$",
            RegexOptions.CultureInvariant);
        if (!m.Success)
            return;
        streetPart = m.Groups["s"].Value.Trim();
        housePart = m.Groups["h"].Value.Trim();
        if (streetPart.Length < 2)
        {
            streetPart = q.Trim();
            housePart = null;
        }
    }

    private static bool CityMatches(string requestedCity, string resultCity)
    {
        var a = requestedCity.Trim().ToLowerInvariant();
        var b = resultCity.Trim().ToLowerInvariant();
        return a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
    }

    private static readonly HashSet<string> NoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "СѓР»РёС†Р°", "СѓР»", "СѓР».", "РїСЂРѕСЃРїРµРєС‚", "РїСЂ-С‚", "С€РѕСЃСЃРµ", "РїРµСЂРµСѓР»РѕРє", "РїРµСЂ", "РїСЂРѕРµР·Рґ",
        "РЅР°Р±РµСЂРµР¶РЅР°СЏ", "РїР»РѕС‰Р°РґСЊ", "РїР»", "Р±СѓР»СЊРІР°СЂ", "Р±-СЂ", "Р°Р»Р»РµСЏ", "РјРёРєСЂРѕСЂР°Р№РѕРЅ", "РјРєСЂ"
    };

    private static bool IsRelevantToUserQuery(string userQuery, string? road, string? display)
    {
        var hay = $"{road} {display}".ToLowerInvariant();
        var tokens = userQuery
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2 && !NoiseTokens.Contains(t))
            .Distinct()
            .ToList();
        if (tokens.Count == 0)
        {
            tokens = userQuery
                .ToLowerInvariant()
                .Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3)
                .Distinct()
                .ToList();
        }
        if (tokens.Count == 0)
            return true;
        foreach (var t in tokens)
        {
            if (!hay.Contains(t, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string GetDedupeKey(JsonElement item, string? displayFallback)
    {
        if (item.TryGetProperty("place_id", out var pid) && pid.ValueKind == JsonValueKind.Number)
            return "pid:" + pid.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);

        var osmType = item.TryGetProperty("osm_type", out var ot) ? ot.GetString() : "";
        if (item.TryGetProperty("osm_id", out var oid) && oid.ValueKind == JsonValueKind.Number &&
            !string.IsNullOrEmpty(osmType))
            return $"{osmType}:{oid.GetInt64()}";

        if (!string.IsNullOrWhiteSpace(displayFallback))
            return "d:" + displayFallback;

        if (item.TryGetProperty("lat", out var la) && item.TryGetProperty("lon", out var lo))
            return $"ll:{la.GetString()}:{lo.GetString()}";

        return Guid.NewGuid().ToString("N");
    }

    private static bool TryReadAddressFields(
        JsonElement item,
        out string? road,
        out string? house,
        out string? resCity,
        out string? state,
        out string? display)
    {
        display = item.TryGetProperty("display_name", out var d) ? d.GetString() : null;
        road = null;
        house = null;
        resCity = null;
        state = null;
        if (!item.TryGetProperty("address", out var address))
            return !string.IsNullOrWhiteSpace(display);

        if (address.TryGetProperty("road", out var roadEl)) road = roadEl.GetString();
        if (string.IsNullOrWhiteSpace(road) && address.TryGetProperty("pedestrian", out var pedEl)) road = pedEl.GetString();
        if (string.IsNullOrWhiteSpace(road) && address.TryGetProperty("footway", out var fwEl)) road = fwEl.GetString();
        if (string.IsNullOrWhiteSpace(road) && address.TryGetProperty("path", out var pathEl)) road = pathEl.GetString();
        if (address.TryGetProperty("house_number", out var houseEl)) house = houseEl.GetString();
        if (address.TryGetProperty("city", out var cityEl)) resCity = cityEl.GetString();
        if (string.IsNullOrWhiteSpace(resCity) && address.TryGetProperty("town", out var townEl)) resCity = townEl.GetString();
        if (string.IsNullOrWhiteSpace(resCity) && address.TryGetProperty("village", out var villageEl)) resCity = villageEl.GetString();
        if (string.IsNullOrWhiteSpace(resCity) && address.TryGetProperty("municipality", out var munEl)) resCity = munEl.GetString();
        if (address.TryGetProperty("state", out var stateEl)) state = stateEl.GetString();
        if (string.IsNullOrWhiteSpace(state) && address.TryGetProperty("region", out var regionEl)) state = regionEl.GetString();
        return true;
    }

    private static object? MapItem(
        string? road,
        string? house,
        string? city,
        string? state,
        string? display,
        JsonElement item)
    {
        var lat = item.TryGetProperty("lat", out var latEl) ? latEl.GetString() : null;
        var lon = item.TryGetProperty("lon", out var lonEl) ? lonEl.GetString() : null;
        return MapItemCore(road, house, city, state, display, lat, lon);
    }

    private static object? MapItemCore(
        string? road,
        string? house,
        string? city,
        string? state,
        string? display,
        string? lat,
        string? lon)
    {
        var primaryLine = !string.IsNullOrWhiteSpace(road)
            ? (string.IsNullOrWhiteSpace(house) ? road : $"{road}, {house}")
            : (display?.Split(',').FirstOrDefault()?.Trim());
        var secondaryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) secondaryParts.Add(city!);
        if (!string.IsNullOrWhiteSpace(state)) secondaryParts.Add(state!);
        var secondaryLine = string.Join(", ", secondaryParts);

        if (string.IsNullOrWhiteSpace(primaryLine) && string.IsNullOrWhiteSpace(display))
            return null;

        return new
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
        };
    }
}
