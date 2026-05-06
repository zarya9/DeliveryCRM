namespace APIDeliveryCRM.Helpers;

public static class VinHelper
{
    public const string AllowedCharacters = "0123456789ABCDEFGHJKLMNPRSTUVWXYZ";

    public static bool IsValid(string? vin, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(vin))
        {
            errorMessage = "РЈРєР°Р¶РёС‚Рµ VIN.";
            return false;
        }

        var s = vin.Trim().ToUpperInvariant();
        if (s.Length != 17)
        {
            errorMessage = "VIN РґРѕР»Р¶РµРЅ СЃРѕРґРµСЂР¶Р°С‚СЊ 17 СЃРёРјРІРѕР»РѕРІ.";
            return false;
        }

        foreach (var c in s)
        {
            if (AllowedCharacters.IndexOf(c) < 0)
            {
                errorMessage = "VIN: РґРѕРїСѓСЃС‚РёРјС‹ С‚РѕР»СЊРєРѕ С†РёС„СЂС‹ Рё Р»Р°С‚РёРЅСЃРєРёРµ Р±СѓРєРІС‹ РёР· РЅР°Р±РѕСЂР° Р±РµР· I, O Рё Q.";
                return false;
            }
        }

        return true;
    }
}
