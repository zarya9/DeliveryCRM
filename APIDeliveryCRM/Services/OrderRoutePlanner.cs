using System;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Services;

public static class OrderRoutePlanner
{
    public static IReadOnlyList<OrderRouteStop> BuildStops(
        DeliveryRouteKind kind,
        Address pickup,
        Address delivery,
        LogisticsHub? originHub,
        LogisticsHub? destinationHub)
    {
        switch (kind)
        {
            case DeliveryRouteKind.LocalUrban:
                return new List<OrderRouteStop>
                {
                    new()
                    {
                        SortOrder = 0,
                        Kind = OrderRouteStopKind.SenderPickup,
                        Title = "Забор у отправителя",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Доставка получателю",
                        Address_id = delivery.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    }
                };

            case DeliveryRouteKind.DirectIntercity:
                return new List<OrderRouteStop>
                {
                    new()
                    {
                        SortOrder = 0,
                        Kind = OrderRouteStopKind.SenderPickup,
                        Title = "Забор (отправитель)",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Вручение (получатель), прямой рейс",
                        Address_id = delivery.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    }
                };

            case DeliveryRouteKind.ViaHub:
                if (originHub == null || destinationHub == null)
                    throw new InvalidOperationException("Для доставки через хабы задайте склад отправления и склад назначения.");

                return new List<OrderRouteStop>
                {
                    new()
                    {
                        SortOrder = 0,
                        Kind = OrderRouteStopKind.SenderPickup,
                        Title = "Забор у отправителя",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.Hub,
                        Title = $"Склад отправления: {originHub.Name}",
                        Address_id = originHub.Address_id,
                        LogisticsHub_id = originHub.ID_LogisticsHub,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 2,
                        Kind = OrderRouteStopKind.Hub,
                        Title = $"Склад назначения: {destinationHub.Name}",
                        Address_id = destinationHub.Address_id,
                        LogisticsHub_id = destinationHub.ID_LogisticsHub,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 3,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Доставка получателю",
                        Address_id = delivery.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    }
                };

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}
