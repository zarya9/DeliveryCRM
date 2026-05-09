using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace APIDeliveryCRM.Services;

public class LogisticsHubService : ILogisticsHubService
{
    private readonly ContextDB _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogisticsHubService(ContextDB context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<LogisticsHub>> GetByCompanyAsync(int companyId)
    {
        var hubs = await _context.LogisticsHubs
            .Where(h => h.Company_id == companyId)
            .Include(h => h.Address)
            .OrderBy(h => h.Name)
            .ToListAsync();

        // Backfill coordinates for old hubs created without lat/lon so they immediately appear on map.
        var changed = false;
        foreach (var hub in hubs.Where(h => h.Address != null))
        {
            if (hub.Address!.Latitude.HasValue && hub.Address.Longitude.HasValue)
                continue;

            var geo = await GeocodeAddressAsync(hub.Address);
            if (!geo.HasValue)
                continue;

            hub.Address.Latitude = (decimal)geo.Value.lat;
            hub.Address.Longitude = (decimal)geo.Value.lon;
            changed = true;
        }

        if (changed)
            await _context.SaveChangesAsync();

        return hubs;
    }

    public async Task<LogisticsHub> CreateAsync(int companyId, int userId, CreateLogisticsHubRequest request)
    {
        var normalizedStreet = NormalizeAddressPart(request.Street);
        var normalizedHouse = NormalizeAddressPart(request.House);
        var normalizedFlat = NormalizeAddressPart(request.Flat);
        var normalizedCity = NormalizeAddressPart(request.City);
        var normalizedRegion = NormalizeAddressPart(request.Region);
        var normalizedPostal = NormalizeAddressPart(request.PostalCode);

        decimal? latitude = request.Latitude;
        decimal? longitude = request.Longitude;
        if (!latitude.HasValue || !longitude.HasValue)
        {
            var geo = await GeocodeAddressAsync(request.City, request.Street, request.House, request.Region, request.PostalCode);
            if (geo.HasValue)
            {
                latitude = (decimal)geo.Value.lat;
                longitude = (decimal)geo.Value.lon;
            }
        }

        // Нормализация в памяти: EF не переводит NormalizeAddressPart в SQL — иначе 500 при создании.
        var companyAddresses = await _context.Addresses
            .Where(a => a.Company_id == companyId)
            .ToListAsync();
        var address = companyAddresses.FirstOrDefault(a =>
            NormalizeAddressPart(a.Street) == normalizedStreet
            && NormalizeAddressPart(a.House) == normalizedHouse
            && NormalizeAddressPart(a.Flat) == normalizedFlat
            && NormalizeAddressPart(a.City) == normalizedCity
            && NormalizeAddressPart(a.Region) == normalizedRegion
            && NormalizeAddressPart(a.PostalCode) == normalizedPostal);

        if (address == null)
        {
            address = new Address
            {
                Street = request.Street,
                House = request.House,
                Flat = request.Flat,
                City = request.City,
                Region = request.Region,
                PostalCode = request.PostalCode,
                Comment = request.Comment,
                Latitude = latitude,
                Longitude = longitude,
                Company_id = companyId,
                User_id = userId
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Keep one canonical address row for hubs and enrich it if geo is missing.
            if ((!address.Latitude.HasValue || !address.Longitude.HasValue) && latitude.HasValue && longitude.HasValue)
            {
                address.Latitude = latitude;
                address.Longitude = longitude;
                await _context.SaveChangesAsync();
            }
        }

        var hub = new LogisticsHub
        {
            Company_id = companyId,
            Name = request.Name.Trim(),
            Address_id = address.ID_Address
        };
        _context.LogisticsHubs.Add(hub);
        await _context.SaveChangesAsync();

        return await _context.LogisticsHubs.Include(h => h.Address)
            .FirstAsync(h => h.ID_LogisticsHub == hub.ID_LogisticsHub);
    }

    public async Task<LogisticsHub?> UpdateAsync(int companyId, int hubId, CreateLogisticsHubRequest request)
    {
        var hub = await _context.LogisticsHubs
            .Include(h => h.Address)
            .FirstOrDefaultAsync(h => h.ID_LogisticsHub == hubId && h.Company_id == companyId);
        if (hub == null)
            return null;

        hub.Name = request.Name.Trim();
        var address = hub.Address;
        address.Street = request.Street.Trim();
        address.House = request.House.Trim();
        address.Flat = request.Flat;
        address.City = request.City;
        address.Region = request.Region;
        address.PostalCode = request.PostalCode;
        address.Comment = request.Comment;

        decimal? latitude = request.Latitude;
        decimal? longitude = request.Longitude;
        if (!latitude.HasValue || !longitude.HasValue)
        {
            var geo = await GeocodeAddressAsync(request.City, request.Street, request.House, request.Region, request.PostalCode);
            if (geo.HasValue)
            {
                latitude = (decimal)geo.Value.lat;
                longitude = (decimal)geo.Value.lon;
            }
        }
        if (latitude.HasValue && longitude.HasValue)
        {
            address.Latitude = latitude;
            address.Longitude = longitude;
        }

        await _context.SaveChangesAsync();
        return hub;
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int companyId, int hubId)
    {
        var hub = await _context.LogisticsHubs
            .FirstOrDefaultAsync(h => h.ID_LogisticsHub == hubId && h.Company_id == companyId);
        if (hub == null)
            return (false, null);

        var hasLinkedOrders = await _context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.OriginHub_id == hubId || o.DestinationHub_id == hubId);
        if (hasLinkedOrders)
            return (false, "Нельзя удалить склад: есть заказы, связанные с этим складом.");

        var hasRouteStops = await _context.OrderRouteStops
            .AsNoTracking()
            .AnyAsync(s => s.LogisticsHub_id == hubId);
        if (hasRouteStops)
            return (false, "Нельзя удалить склад: он используется в маршрутах.");

        _context.LogisticsHubs.Remove(hub);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    private static string NormalizeAddressPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var normalized = value.Trim().ToLowerInvariant();
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        return normalized;
    }

    private async Task<(double lat, double lon)?> GeocodeAddressAsync(Address address)
        => await GeocodeAddressAsync(address.City, address.Street, address.House, address.Region, address.PostalCode);

    private async Task<(double lat, double lon)?> GeocodeAddressAsync(string? city, string? street, string? house, string? region, string? postalCode)
    {
        var detailedParts = new[] { postalCode, city, street, house, region }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        var shortParts = new[] { city, street, house }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        if (detailedParts.Length == 0 && shortParts.Length == 0)
            return null;

        var queries = new[]
        {
            detailedParts.Length > 0 ? string.Join(", ", detailedParts) : null,
            shortParts.Length > 0 ? string.Join(", ", shortParts) : null
        }.Where(q => !string.IsNullOrWhiteSpace(q)).Select(q => q!).Distinct(StringComparer.OrdinalIgnoreCase);

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryCRM/1.0 (logistics-hub-geocoder)");
            client.Timeout = TimeSpan.FromSeconds(8);
            foreach (var query in queries)
            {
                var primary = $"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=ru&q={Uri.EscapeDataString(query)}";
                var fallback = $"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q={Uri.EscapeDataString(query)}";
                foreach (var url in new[] { primary, fallback })
                {
                    using var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                        continue;

                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    var first = doc.RootElement.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind != JsonValueKind.Object)
                        continue;

                    var latRaw = first.TryGetProperty("lat", out var latEl) ? latEl.GetString() : null;
                    var lonRaw = first.TryGetProperty("lon", out var lonEl) ? lonEl.GetString() : null;
                    if (!double.TryParse(latRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                        continue;
                    if (!double.TryParse(lonRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                        continue;

                    return (lat, lon);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
