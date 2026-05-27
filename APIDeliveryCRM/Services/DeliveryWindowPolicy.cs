namespace APIDeliveryCRM.Services;

/// <summary>
/// Календарные правила обещанной даты доставки (окно для клиента).
/// Время — Europe/Moscow; после 18:00 заказы обрабатываются со следующего календарного дня.
/// </summary>
public static class DeliveryWindowPolicy
{
    public const int CutoffHourLocal = 18;

    public sealed record Result(
        DateTime? EtaUtc,
        DateTime SlaDueUtc,
        string DisplayText);

    public static Result Compute(DateTime createdUtc, byte priority)
    {
        var tz = GetMoscowTimeZone();
        var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc), tz);
        var orderDate = DateOnly.FromDateTime(createdLocal);
        var afterCutoff = createdLocal.Hour >= CutoffHourLocal;

        // Базовый «рабочий» день: сегодня или со следующего дня, если заказ после 18:00.
        var baseDate = orderDate.AddDays(afterCutoff ? 1 : 0);
        var normalizedPriority = priority >= 2 ? (byte)2 : priority;

        var targetDay = normalizedPriority switch
        {
            // Критический: в baseDate (сегодня до 18:00, иначе завтра).
            2 => baseDate,
            // Срочный: на следующий день от baseDate.
            1 => baseDate.AddDays(1),
            // Обычный: не позднее чем через 2 дня от baseDate (минимум послезавтра при заказе до 18:00).
            _ => baseDate.AddDays(2)
        };

        var dayStartLocal = targetDay.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Unspecified);
        var dayEndLocal = targetDay.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Unspecified);
        var slaDueUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, tz);

        DateTime? etaUtc = normalizedPriority switch
        {
            0 => null,
            _ => TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, tz)
        };

        var display = FormatDisplayText(normalizedPriority, targetDay, createdLocal);
        return new Result(etaUtc, slaDueUtc, display);
    }

    public static string FormatDisplayText(byte priority, DateTime? slaDueUtc, DateTime? etaUtc, DateTime createdUtc)
    {
        if (!slaDueUtc.HasValue && !etaUtc.HasValue)
            return "дата уточняется";

        var tz = GetMoscowTimeZone();
        var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc), tz);

        DateOnly targetDay;
        if (slaDueUtc.HasValue)
        {
            var dueLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(slaDueUtc.Value, DateTimeKind.Utc), tz);
            targetDay = DateOnly.FromDateTime(dueLocal);
        }
        else if (etaUtc.HasValue)
        {
            var etaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(etaUtc.Value, DateTimeKind.Utc), tz);
            targetDay = DateOnly.FromDateTime(etaLocal);
        }
        else
            return "дата уточняется";

        var normalizedPriority = priority >= 2 ? (byte)2 : priority;
        return FormatDisplayText(normalizedPriority, targetDay, createdLocal);
    }

    private static string FormatDisplayText(byte priority, DateOnly targetDay, DateTime createdLocal)
    {
        var dateText = FormatDayMonthRu(targetDay);

        return priority switch
        {
            0 => $"не позднее {dateText}",
            2 => $"доставка {dateText}",
            1 => $"доставка {dateText}",
            _ => dateText
        };
    }

    public static string FormatDayMonthRu(DateOnly date)
        => $"{date.Day:00} {GetMonthRu(date.Month)}";

    public static string FormatDayMonthRu(DateTime dt)
        => FormatDayMonthRu(DateOnly.FromDateTime(dt));

    private static string GetMonthRu(int month) => month switch
    {
        1 => "января",
        2 => "февраля",
        3 => "марта",
        4 => "апреля",
        5 => "мая",
        6 => "июня",
        7 => "июля",
        8 => "августа",
        9 => "сентября",
        10 => "октября",
        11 => "ноября",
        12 => "декабря",
        _ => string.Empty
    };

    private static TimeZoneInfo GetMoscowTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        }
    }
}
