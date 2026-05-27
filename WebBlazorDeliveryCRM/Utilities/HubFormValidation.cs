using System.Globalization;
using System.Text.RegularExpressions;

namespace WebBlazorDeliveryCRM.Utilities;

public static class HubFormValidation
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly Regex PostalRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    public sealed record NormalizedValues(
        string Name,
        string Street,
        string House,
        string? Flat,
        string City,
        string? Region,
        string? PostalCode);

    public sealed record FieldErrors(
        string? Name,
        string? City,
        string? Street,
        string? House,
        string? Flat,
        string? Region,
        string? PostalCode);

    public static bool TryValidate(
        string name,
        string street,
        string house,
        string? flat,
        string? city,
        string? region,
        string? postalCode,
        out NormalizedValues normalized,
        out FieldErrors errors,
        out string? summary)
    {
        normalized = new NormalizedValues(
            CapitalizeWords(name),
            CapitalizeWords(street),
            house.Trim(),
            string.IsNullOrWhiteSpace(flat) ? null : flat.Trim(),
            CapitalizeWords(city),
            string.IsNullOrWhiteSpace(region) ? null : CapitalizeWords(region),
            string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim());

        string? nameErr = null;
        string? cityErr = null;
        string? streetErr = null;
        string? houseErr = null;
        string? flatErr = null;
        string? regionErr = null;
        string? postalErr = null;

        if (normalized.Name.Length < 2)
            nameErr = "Минимум 2 символа.";

        if (normalized.City.Length < 2)
            cityErr = "Укажите город (минимум 2 символа).";

        if (normalized.Street.Length < 2)
            streetErr = "Улица — минимум 2 символа.";

        if (!IsValidHouse(normalized.House))
            houseErr = "Укажите номер дома (например 12 или 12А).";

        if (normalized.Flat is { Length: 1 })
            flatErr = "Квартира/офис: минимум 2 символа или оставьте пустым.";

        if (normalized.Region is { Length: 1 })
            regionErr = "Регион: минимум 2 символа или оставьте пустым.";

        if (normalized.PostalCode is not null && !PostalRegex.IsMatch(normalized.PostalCode))
            postalErr = "Индекс — 6 цифр (например 420000).";

        errors = new FieldErrors(nameErr, cityErr, streetErr, houseErr, flatErr, regionErr, postalErr);

        var parts = new[] { nameErr, cityErr, streetErr, houseErr, flatErr, regionErr, postalErr }
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();
        if (parts.Count == 0)
        {
            summary = null;
            return true;
        }

        summary = string.Join(" ", parts);
        return false;
    }

    private static bool IsValidHouse(string house)
    {
        if (string.IsNullOrWhiteSpace(house))
            return false;
        house = house.Trim();
        if (house.Length > 20)
            return false;
        return house.Any(char.IsDigit);
    }

    public static string CapitalizeWords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Ru.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }
}
