using System.Linq;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Helpers;

public static class HubOccupancyHelper
{
    public static bool IsOrderAtHub(Order order, int hubId)
    {
        if (order.RouteStops == null || order.RouteStops.Count == 0)
            return false;

        var stops = order.RouteStops.OrderBy(s => s.SortOrder).ToList();
        foreach (var s in stops.Where(x =>
                     x.LogisticsHub_id == hubId && x.Kind == OrderRouteStopKind.Hub))
        {
            if (s.Status == OrderRouteStopStatus.InProgress)
                return true;
            if (s.Status == OrderRouteStopStatus.Pending)
            {
                var priorOk = stops
                    .Where(x => x.SortOrder < s.SortOrder)
                    .All(x => x.Status == OrderRouteStopStatus.Completed
                              || x.Status == OrderRouteStopStatus.Skipped);
                if (priorOk)
                    return true;
            }
        }

        return false;
    }

    public static string FormatClientName(ClientProfile? client)
    {
        if (client?.User == null)
            return "РљР»РёРµРЅС‚";
        return $"{client.User.FName} {client.User.Name}".Trim();
    }

    public static string FormatDeliveryLine(Address? addr)
    {
        if (addr == null)
            return "вЂ”";
        var parts = new[] { addr.City, addr.Street, addr.House }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());
        var s = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(s) ? "вЂ”" : s;
    }
}
