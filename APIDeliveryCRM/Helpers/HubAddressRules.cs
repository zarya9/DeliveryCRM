using System.Globalization;
using System.Text.RegularExpressions;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Helpers;

public static class HubAddressRules
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly Regex PostalRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    public static bool TryValidate(CreateLogisticsHubRequest request, out string? errorMessage)
    {
        errorMessage = null;
        if (request is null)
        {
            errorMessage = "Пустой запрос.";
            return false;
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            errors.Add("Название склада: минимум 2 символа.");
        if (string.IsNullOrWhiteSpace(request.City) || request.City.Trim().Length < 2)
            errors.Add("Город: минимум 2 символа.");
        if (string.IsNullOrWhiteSpace(request.Street) || request.Street.Trim().Length < 2)
            errors.Add("Улица: минимум 2 символа.");
        if (!IsValidHouse(request.House))
            errors.Add("Дом: укажите номер (например 12 или 12А).");
        if (request.Flat is { Length: 1 })
            errors.Add("Квартира/офис: минимум 2 символа или оставьте пустым.");
        if (request.Region is { Length: 1 })
            errors.Add("Регион: минимум 2 символа или оставьте пустым.");
        if (!string.IsNullOrWhiteSpace(request.PostalCode) && !PostalRegex.IsMatch(request.PostalCode.Trim()))
            errors.Add("Индекс: 6 цифр.");

        if (errors.Count == 0)
            return true;

        errorMessage = string.Join(" ", errors);
        return false;
    }

    public static void ApplyFormatting(CreateLogisticsHubRequest request)
    {
        request.Name = CapitalizeWords(request.Name);
        request.Street = CapitalizeWords(request.Street);
        request.City = CapitalizeWords(request.City);
        request.House = request.House?.Trim() ?? string.Empty;
        request.Flat = string.IsNullOrWhiteSpace(request.Flat) ? null : request.Flat.Trim();
        request.Region = string.IsNullOrWhiteSpace(request.Region) ? null : CapitalizeWords(request.Region);
        request.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        request.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
    }

    private static bool IsValidHouse(string? house)
    {
        if (string.IsNullOrWhiteSpace(house))
            return false;
        house = house.Trim();
        if (house.Length > 20)
            return false;
        return house.Any(char.IsDigit);
    }

    private static string CapitalizeWords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Ru.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }
}
