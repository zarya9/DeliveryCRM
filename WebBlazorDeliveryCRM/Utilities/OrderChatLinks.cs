using System.Text.RegularExpressions;

namespace WebBlazorDeliveryCRM.Utilities;

public static class OrderChatLinks
{
    /// <summary>Маркер в тексте сообщения: [[order:123]]</summary>
    private static readonly Regex OrderMarkerRegex = new(
        @"\[\[order:(\d+)\]\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Старые сообщения с полным URL (localhost и т.д.).</summary>
    private static readonly Regex LegacyOrderUrlRegex = new(
        @"https?://[^\s<>""']+/manager/orders\?openOrderId=(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public const string OpenOrderQueryKey = "openOrderId";

    public static string BuildOrderMarker(int orderId) => $"[[order:{orderId}]]";

    public static string BuildCustomerIntroMessage(string orderName, int orderId)
    {
        var safeName = (orderName ?? string.Empty).Trim().Replace("\"", "'");
        if (safeName.Length > 500)
            safeName = safeName[..500];
        return $"Здравствуйте! Пишу Вам, касаемо заказа \"{safeName}\" {BuildOrderMarker(orderId)}";
    }

    public static bool TryParseOrderIdFromText(string? text, out int orderId)
    {
        orderId = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var marker = OrderMarkerRegex.Match(text);
        if (marker.Success && int.TryParse(marker.Groups[1].Value, out orderId) && orderId > 0)
            return true;

        var legacy = LegacyOrderUrlRegex.Match(text);
        if (legacy.Success && int.TryParse(legacy.Groups[1].Value, out orderId) && orderId > 0)
            return true;

        return false;
    }

    public static IReadOnlyList<ChatMessageSegment> SplitMessage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<ChatMessageSegment>();

        var matches = new List<(int Index, int Length, int OrderId)>();

        foreach (Match m in OrderMarkerRegex.Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value, out var id) && id > 0)
                matches.Add((m.Index, m.Length, id));
        }

        foreach (Match m in LegacyOrderUrlRegex.Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value, out var id) && id > 0)
                matches.Add((m.Index, m.Length, id));
        }

        matches = matches
            .OrderBy(m => m.Index)
            .ToList();

        // Убираем пересечения (приоритет — более ранний матч).
        var filtered = new List<(int Index, int Length, int OrderId)>();
        var coveredUntil = -1;
        foreach (var m in matches)
        {
            if (m.Index < coveredUntil)
                continue;
            filtered.Add(m);
            coveredUntil = m.Index + m.Length;
        }

        if (filtered.Count == 0)
            return new[] { new ChatMessageSegment(ChatMessageSegmentKind.Text, text) };

        var segments = new List<ChatMessageSegment>();
        var lastIndex = 0;
        foreach (var m in filtered)
        {
            if (m.Index > lastIndex)
            {
                segments.Add(new ChatMessageSegment(
                    ChatMessageSegmentKind.Text,
                    text.Substring(lastIndex, m.Index - lastIndex)));
            }

            segments.Add(new ChatMessageSegment(
                ChatMessageSegmentKind.OrderLink,
                string.Empty,
                m.OrderId));

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < text.Length)
        {
            segments.Add(new ChatMessageSegment(
                ChatMessageSegmentKind.Text,
                text.Substring(lastIndex)));
        }

        return segments;
    }

    public static string FormatOrderLinkLabel(int orderId, bool forStaff)
        => forStaff ? "Открыть карточку заказа" : $"Заказ №{orderId}";

    /// <summary>Краткий превью для списка чатов (без маркеров и URL).</summary>
    public static string FormatMessagePreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var preview = text;
        preview = OrderMarkerRegex.Replace(preview, m =>
            int.TryParse(m.Groups[1].Value, out var id) ? $"· Заказ №{id}" : string.Empty);
        preview = LegacyOrderUrlRegex.Replace(preview, m =>
            int.TryParse(m.Groups[1].Value, out var id) ? $"· Заказ №{id}" : string.Empty);

        return preview.Trim();
    }
}

public enum ChatMessageSegmentKind
{
    Text,
    OrderLink
}

public sealed record ChatMessageSegment(
    ChatMessageSegmentKind Kind,
    string Text,
    int OrderId = 0,
    string? Url = null);
