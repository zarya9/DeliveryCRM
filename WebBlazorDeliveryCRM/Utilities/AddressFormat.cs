using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Utilities;

public static class AddressFormat
{
    /// <summary>
    /// Одна строка для отображения адреса клиенту (забор / доставка).
    /// </summary>
    public static string OneLine(AddressShortDto? a)
    {
        if (a is null)
            return "—";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.PostalCode))
            parts.Add(a.PostalCode.Trim());
        if (!string.IsNullOrWhiteSpace(a.Region) && !string.Equals(a.Region, a.City, StringComparison.OrdinalIgnoreCase))
            parts.Add(a.Region.Trim());
        if (!string.IsNullOrWhiteSpace(a.City))
            parts.Add(a.City.Trim());

        var streetHouse = StreetHouse(a);
        if (!string.IsNullOrWhiteSpace(streetHouse))
            parts.Add(streetHouse);

        if (parts.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(a.Comment))
                return string.Join(", ", parts) + " · " + a.Comment.Trim();
            return string.Join(", ", parts);
        }

        if (a.Latitude is { } lat && a.Longitude is { } lon)
            return $"Координаты: {lat:F5}, {lon:F5}";
        return "—";
    }

    private static string StreetHouse(AddressShortDto a)
    {
        var street = (a.Street ?? "").Trim();
        var house = (a.House ?? "").Trim();
        var flat = string.IsNullOrWhiteSpace(a.Flat) ? "" : $", кв. {a.Flat.Trim()}";

        if (string.IsNullOrEmpty(street) && string.IsNullOrEmpty(house))
            return "";
        if (string.IsNullOrEmpty(street))
            return $"д. {house}{flat}";
        if (string.IsNullOrEmpty(house))
            return street + flat;
        return $"{street}, {house}{flat}";
    }

    /// <summary>
    /// Строка запроса к геокодеру (город, улица, дом).
    /// </summary>
    public static string GeocodeSearchQuery(AddressShortDto? a)
    {
        if (a is null)
            return "";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.City))
            parts.Add(a.City.Trim());
        if (!string.IsNullOrWhiteSpace(a.Region) &&
            !string.Equals(a.Region.Trim(), a.City?.Trim(), StringComparison.OrdinalIgnoreCase))
            parts.Add(a.Region.Trim());
        if (!string.IsNullOrWhiteSpace(a.PostalCode))
            parts.Add(a.PostalCode.Trim());
        if (!string.IsNullOrWhiteSpace(a.Street))
            parts.Add(a.Street.Trim());
        if (!string.IsNullOrWhiteSpace(a.House))
            parts.Add(a.House.Trim());
        return string.Join(", ", parts);
    }
}
