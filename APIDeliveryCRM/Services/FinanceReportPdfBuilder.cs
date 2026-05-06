using System.IO;
using APIDeliveryCRM.Responses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace APIDeliveryCRM.Services;

internal static class FinanceReportPdfBuilder
{
    public static byte[] Build(FinanceDashboardResponse d)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Финансовый отчёт").FontSize(18).SemiBold();
                    col.Item().Text($"Период (UTC): {d.PeriodFromUtc:yyyy-MM-dd HH:mm} — {d.PeriodToUtc:yyyy-MM-dd HH:mm}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Заказов создано: {d.OrdersCreatedInPeriod}");
                    col.Item().Text($"Выручка по доставленным: {d.RevenueDeliveredInPeriod:N0} ₽");
                    col.Item().Text($"В срок: {d.OnTimePercent:0.0}%");
                    col.Item().Text($"Опоздания: {d.LatePercent:0.0}%");
                    col.Item().Text($"Среднее время доставки, ч: {d.AvgDeliveryHours:0.0}");
                    col.Item().Text($"Топлива израсходовано (оценка), л: {d.EstimatedFuelUsedLiters:0.0}");
                    col.Item().Text($"Экономия топлива (оценка), л: {d.EstimatedFuelSavedLiters:0.0} ({d.EstimatedFuelSavingsPercent:0.0}%)");
                    col.Item().Text($"Модель расхода: {d.FuelConsumptionLitersPer100Km:0.0} л/100 км").FontColor(Colors.Grey.Darken2);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Сформировано в DeliveryCRM");
                    x.Span(" | ");
                    x.CurrentPageNumber();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
