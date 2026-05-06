namespace WebBlazorDeliveryCRM.Utilities;

public static class VinHelper
{
    public const string AllowedCharacters = "0123456789ABCDEFGHJKLMNPRSTUVWXYZ";

    public static bool IsValid(string? vin, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(vin))
        {
            errorMessage = "Укажите VIN.";
            return false;
        }

        var s = vin.Trim().ToUpperInvariant();
        if (s.Length != 17)
        {
            errorMessage = "VIN должен содержать 17 символов.";
            return false;
        }

        foreach (var c in s)
        {
            if (AllowedCharacters.IndexOf(c) < 0)
            {
                errorMessage = "VIN: допустимы только цифры и латинские буквы без I, O и Q.";
                return false;
            }
        }

        return true;
    }

    public static string SanitizeInput(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        var chars = raw.Trim().ToUpperInvariant().Where(c => AllowedCharacters.Contains(c, StringComparison.Ordinal)).ToArray();
        return new string(chars);
    }
}
