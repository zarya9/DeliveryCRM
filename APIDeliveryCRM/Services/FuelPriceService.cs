using System.Text.Json;
using APIDeliveryCRM.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace APIDeliveryCRM.Services;

public class FuelPriceService : IFuelPriceService
{
    private const string CacheKey = "fuel-prices-rub-v1";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FuelPriceService> _logger;

    public FuelPriceService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<FuelPriceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<decimal> GetPriceRubPerLiterAsync(string? fuelTypeName, CancellationToken cancellationToken = default)
    {
        var prices = await GetPricesAsync(cancellationToken);
        return ResolvePriceByFuelTypeName(fuelTypeName, prices);
    }

    private async Task<Dictionary<string, decimal>> GetPricesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? cached) && cached is not null && cached.Count > 0)
            return cached;

        var fallback = GetDefaultPrices();
        var apiUrl = _configuration["FuelPricing:ExternalApiUrl"];
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            _cache.Set(CacheKey, fallback, TimeSpan.FromHours(6));
            return fallback;
        }

        try
        {
            var timeoutSeconds = _configuration.GetValue("FuelPricing:RequestTimeoutSeconds", 8);
            var cacheMinutes = Math.Max(5, _configuration.GetValue("FuelPricing:CacheMinutes", 360));
            var apiKey = _configuration["FuelPricing:ApiKey"];

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 3, 25));

            using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);

            using var resp = await client.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fuel API returned non-success status {StatusCode}. Using fallback.", (int)resp.StatusCode);
                _cache.Set(CacheKey, fallback, TimeSpan.FromMinutes(20));
                return fallback;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var parsed = ParsePrices(doc.RootElement);

            if (parsed.Count == 0)
            {
                _logger.LogWarning("Fuel API response contains no recognizable prices. Using fallback.");
                _cache.Set(CacheKey, fallback, TimeSpan.FromMinutes(20));
                return fallback;
            }

            var merged = MergeWithFallback(parsed, fallback);
            _cache.Set(CacheKey, merged, TimeSpan.FromMinutes(cacheMinutes));
            return merged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fuel API unavailable. Using fallback prices.");
            _cache.Set(CacheKey, fallback, TimeSpan.FromMinutes(20));
            return fallback;
        }
    }

    private static Dictionary<string, decimal> ParsePrices(JsonElement root)
    {
        var source = root;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("prices", out var pricesObj) &&
            pricesObj.ValueKind == JsonValueKind.Object)
        {
            source = pricesObj;
        }

        if (source.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in source.EnumerateObject())
        {
            if (!TryReadDecimal(p.Value, out var value) || value <= 0m)
                continue;

            var key = NormalizeFuelKey(p.Name);
            if (key != "по_умолчанию")
                result[key] = value;
        }

        return result;
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = 0m;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                element.GetString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value),
            _ => false
        };
    }

    private static decimal ResolvePriceByFuelTypeName(string? fuelTypeName, IReadOnlyDictionary<string, decimal> prices)
    {
        var key = NormalizeFuelKey(fuelTypeName);
        if (prices.TryGetValue(key, out var exact))
            return exact;

        if (prices.TryGetValue("по_умолчанию", out var fallback))
            return fallback;

        return 60m;
    }

    private static string NormalizeFuelKey(string? fuelTypeName)
    {
        if (string.IsNullOrWhiteSpace(fuelTypeName))
            return "по_умолчанию";

        var name = fuelTypeName.Trim().ToLowerInvariant();
        if (name is "default" or "по_умолчанию")
            return "по_умолчанию";
        if (name.Contains("водород", StringComparison.Ordinal) || name.Contains("hydrogen", StringComparison.Ordinal) || name.Contains("fcev", StringComparison.Ordinal))
            return "водород";
        if (name.Contains("элект", StringComparison.Ordinal) || name.Contains("electr", StringComparison.Ordinal) || name.Contains("bev", StringComparison.Ordinal))
            return "электро";
        if (name.Contains("гибрид", StringComparison.Ordinal) || name.Contains("hybrid", StringComparison.Ordinal) || name.Contains("phev", StringComparison.Ordinal) || name.Contains("mhev", StringComparison.Ordinal))
            return "гибрид";
        if (name.Contains("биодиз", StringComparison.Ordinal) || name.Contains("biodiesel", StringComparison.Ordinal))
            return "биодизель";
        if (name.Contains("этанол", StringComparison.Ordinal) || name.Contains("e85", StringComparison.Ordinal) || name.Contains("ethanol", StringComparison.Ordinal))
            return "этанол";
        if (name.Contains("98", StringComparison.Ordinal) || name.Contains("100", StringComparison.Ordinal))
            return "бензин_98";
        if (name.Contains("95", StringComparison.Ordinal))
            return "бензин_95";
        if (name.Contains("92", StringComparison.Ordinal))
            return "бензин_92";
        if (name.Contains("diesel", StringComparison.Ordinal) || name.Contains("диз", StringComparison.Ordinal))
            return "дизель";
        if (name.Contains("cng", StringComparison.Ordinal) || name.Contains("метан", StringComparison.Ordinal))
            return "газ_cng";
        if (name.Contains("газ", StringComparison.Ordinal) || name.Contains("lpg", StringComparison.Ordinal))
            return "газ_lpg";
        if (name.Contains("бенз", StringComparison.Ordinal) || name.Contains("petrol", StringComparison.Ordinal))
            return "бензин_95";

        return "по_умолчанию";
    }

    private static Dictionary<string, decimal> MergeWithFallback(
        IReadOnlyDictionary<string, decimal> parsed,
        IReadOnlyDictionary<string, decimal> fallback)
    {
        var merged = new Dictionary<string, decimal>(fallback, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in parsed)
            merged[kv.Key] = kv.Value;
        return merged;
    }

    private static Dictionary<string, decimal> GetDefaultPrices() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["дизель"] = 70m,
            ["бензин_92"] = 58m,
            ["бензин_95"] = 62m,
            ["бензин_98"] = 73m,
            ["газ_lpg"] = 33m,
            ["газ_cng"] = 30m,
            ["гибрид"] = 62m,
            ["электро"] = 0m,
            ["водород"] = 85m,
            ["биодизель"] = 69m,
            ["этанол"] = 56m,
            ["по_умолчанию"] = 60m
        };
}
