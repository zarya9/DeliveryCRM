using System;
using System.IO;
using System.Linq;
using APIDeliveryCRM.Responses;
using ClosedXML.Excel;

namespace APIDeliveryCRM.Services;

internal static class FinanceReportExcelBuilder
{
    public static byte[] Build(FinanceDashboardResponse d)
    {
        using var wb = new XLWorkbook();
        var summary = wb.Worksheets.Add("Сводка");
        summary.Cell(1, 1).Value = "Период (UTC)";
        summary.Cell(1, 2).Value = $"{d.PeriodFromUtc:yyyy-MM-dd HH:mm} — {d.PeriodToUtc:yyyy-MM-dd HH:mm}";
        summary.Cell(2, 1).Value = "SLA вовремя, ч";
        summary.Cell(2, 2).Value = d.SlaOnTimeHours;
        summary.Cell(3, 1).Value = "SLA опоздание, ч";
        summary.Cell(3, 2).Value = d.SlaLateHours;
        summary.Cell(4, 1).Value = "Заказов создано";
        summary.Cell(4, 2).Value = d.OrdersCreatedInPeriod;
        summary.Cell(5, 1).Value = "Выручка (доставлено)";
        summary.Cell(5, 2).Value = (double)d.RevenueDeliveredInPeriod;
        summary.Cell(6, 1).Value = "Средний чек";
        summary.Cell(6, 2).Value = (double)d.AvgCheckDeliveredInPeriod;
        summary.Cell(7, 1).Value = "Оплаченных доставок";
        summary.Cell(7, 2).Value = d.PaidDeliveredCount;
        summary.Cell(8, 1).Value = "Доля оплаченных, %";
        summary.Cell(8, 2).Value = (double)d.PaidSharePercent;
        summary.Cell(9, 1).Value = "Среднее время доставки, ч";
        summary.Cell(9, 2).Value = d.AvgDeliveryHours;
        summary.Cell(10, 1).Value = "Вовремя %";
        summary.Cell(10, 2).Value = d.OnTimePercent;
        summary.Cell(11, 1).Value = "Опоздания %";
        summary.Cell(11, 2).Value = d.LatePercent;
        summary.Cell(12, 1).Value = "Расход модели, л/100км";
        summary.Cell(12, 2).Value = d.FuelConsumptionLitersPer100Km;
        summary.Cell(13, 1).Value = "Топлива израсходовано (оценка), л";
        summary.Cell(13, 2).Value = d.EstimatedFuelUsedLiters;
        summary.Cell(14, 1).Value = "Экономия топлива (оценка), л";
        summary.Cell(14, 2).Value = d.EstimatedFuelSavedLiters;
        summary.Cell(15, 1).Value = "Экономия топлива, %";
        summary.Cell(15, 2).Value = d.EstimatedFuelSavingsPercent;

        var daily = wb.Worksheets.Add("По дням");
        daily.Cell(1, 1).Value = "Дата";
        daily.Cell(1, 2).Value = "Заказов";
        daily.Cell(1, 3).Value = "Выручка";
        var row = 2;
        foreach (var x in d.OrdersByDay.OrderBy(x => x.Date))
        {
            daily.Cell(row, 1).Value = x.Date;
            daily.Cell(row, 2).Value = x.Count;
            daily.Cell(row, 3).Value = (double)x.Revenue;
            row++;
        }

        var st = wb.Worksheets.Add("Статусы");
        st.Cell(1, 1).Value = "Статус";
        st.Cell(1, 2).Value = "Кол-во";
        st.Cell(1, 3).Value = "Доля %";
        row = 2;
        foreach (var x in d.StatusRows)
        {
            st.Cell(row, 1).Value = x.Status;
            st.Cell(row, 2).Value = x.Count;
            st.Cell(row, 3).Value = x.Share;
            row++;
        }

        var couriers = wb.Worksheets.Add("Курьеры");
        couriers.Cell(1, 1).Value = "Курьер";
        couriers.Cell(1, 2).Value = "Доставок";
        couriers.Cell(1, 3).Value = "Выручка";
        couriers.Cell(1, 4).Value = "Рейтинг";
        row = 2;
        foreach (var x in d.CourierRows)
        {
            couriers.Cell(row, 1).Value = x.Name;
            couriers.Cell(row, 2).Value = x.Delivered;
            couriers.Cell(row, 3).Value = (double)x.Revenue;
            couriers.Cell(row, 4).Value = (double)x.Rating;
            row++;
        }

        var mgr = wb.Worksheets.Add("Менеджеры (лиды)");
        mgr.Cell(1, 1).Value = "Менеджер";
        mgr.Cell(1, 2).Value = "Лидов";
        mgr.Cell(1, 3).Value = "Конверсий";
        mgr.Cell(1, 4).Value = "Конверсия %";
        row = 2;
        foreach (var x in d.ManagerRows)
        {
            mgr.Cell(row, 1).Value = x.Manager;
            mgr.Cell(row, 2).Value = x.Leads;
            mgr.Cell(row, 3).Value = x.Conversions;
            mgr.Cell(row, 4).Value = x.ConversionPercent;
            row++;
        }

        var clients = wb.Worksheets.Add("Клиенты");
        clients.Cell(1, 1).Value = "Клиент";
        clients.Cell(1, 2).Value = "Заказов";
        clients.Cell(1, 3).Value = "Выручка";
        row = 2;
        foreach (var x in d.ClientRows)
        {
            clients.Cell(row, 1).Value = x.Name;
            clients.Cell(row, 2).Value = x.Orders;
            clients.Cell(row, 3).Value = (double)x.Revenue;
            row++;
        }

        foreach (var ws in wb.Worksheets)
        {
            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
