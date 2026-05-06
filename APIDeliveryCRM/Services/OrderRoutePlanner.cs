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
                        Title = "Р—Р°Р±РѕСЂ Сѓ РѕС‚РїСЂР°РІРёС‚РµР»СЏ",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Р”РѕСЃС‚Р°РІРєР° РїРѕР»СѓС‡Р°С‚РµР»СЋ",
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
                        Title = "Р—Р°Р±РѕСЂ (РѕС‚РїСЂР°РІРёС‚РµР»СЊ)",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Р’СЂСѓС‡РµРЅРёРµ (РїРѕР»СѓС‡Р°С‚РµР»СЊ), РїСЂСЏРјРѕР№ СЂРµР№СЃ",
                        Address_id = delivery.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    }
                };

            case DeliveryRouteKind.ViaHub:
                if (originHub == null || destinationHub == null)
                    throw new InvalidOperationException("Р”Р»СЏ РґРѕСЃС‚Р°РІРєРё С‡РµСЂРµР· С…Р°Р±С‹ Р·Р°РґР°Р№С‚Рµ СЃРєР»Р°Рґ РѕС‚РїСЂР°РІР»РµРЅРёСЏ Рё СЃРєР»Р°Рґ РЅР°Р·РЅР°С‡РµРЅРёСЏ.");

                return new List<OrderRouteStop>
                {
                    new()
                    {
                        SortOrder = 0,
                        Kind = OrderRouteStopKind.SenderPickup,
                        Title = "Р—Р°Р±РѕСЂ Сѓ РѕС‚РїСЂР°РІРёС‚РµР»СЏ",
                        Address_id = pickup.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 1,
                        Kind = OrderRouteStopKind.Hub,
                        Title = $"РЎРєР»Р°Рґ РѕС‚РїСЂР°РІР»РµРЅРёСЏ: {originHub.Name}",
                        Address_id = originHub.Address_id,
                        LogisticsHub_id = originHub.ID_LogisticsHub,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 2,
                        Kind = OrderRouteStopKind.Hub,
                        Title = $"РЎРєР»Р°Рґ РЅР°Р·РЅР°С‡РµРЅРёСЏ: {destinationHub.Name}",
                        Address_id = destinationHub.Address_id,
                        LogisticsHub_id = destinationHub.ID_LogisticsHub,
                        Status = OrderRouteStopStatus.Pending
                    },
                    new()
                    {
                        SortOrder = 3,
                        Kind = OrderRouteStopKind.RecipientDelivery,
                        Title = "Р”РѕСЃС‚Р°РІРєР° РїРѕР»СѓС‡Р°С‚РµР»СЋ",
                        Address_id = delivery.ID_Address,
                        Status = OrderRouteStopStatus.Pending
                    }
                };

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}
