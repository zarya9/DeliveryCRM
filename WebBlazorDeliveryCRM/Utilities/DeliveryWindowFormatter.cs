using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Utilities;

public static class DeliveryWindowFormatter
{
    public static string Format(OrderDto order)
    {
        if (order is null)
            return "—";

        return Format(order.Priority, order.Sla_due_at, order.Eta_at, order.Created_at);
    }

    public static string Format(byte priority, DateTime? slaDueUtc, DateTime? etaUtc, DateTime createdUtc)
    {
        if (!slaDueUtc.HasValue && !etaUtc.HasValue)
            return "—";

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
        else
        {
            var etaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(etaUtc!.Value, DateTimeKind.Utc), tz);
            targetDay = DateOnly.FromDateTime(etaLocal);
        }

        var normalizedPriority = priority >= 2 ? (byte)2 : priority;
        var dateText = FormatDayMonthRu(targetDay);

        return normalizedPriority switch
        {
            0 => $"не позднее {dateText}",
            2 => $"доставка {dateText}",
            1 => $"доставка {dateText}",
            _ => dateText
        };
    }

    private static string FormatDayMonthRu(DateOnly date)
        => $"{date.Day:00} {GetMonthRu(date.Month)}";

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
