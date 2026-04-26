namespace APIDeliveryCRM.Model;

/// <summary>Тип логистической схемы для заказа.</summary>
public enum DeliveryRouteKind : byte
{
    /// <summary>Город: забрать и отвезти получателю (один сегмент или последняя миля).</summary>
    LocalUrban = 1,

    /// <summary>Через склады: отправитель → хаб отправления → хаб назначения → получатель.</summary>
    ViaHub = 2,

    /// <summary>Прямой межгород без промежуточного склада (один длинный сегмент).</summary>
    DirectIntercity = 3
}

/// <summary>Тип остановки в упорядоченном маршруте.</summary>
public enum OrderRouteStopKind : byte
{
    /// <summary>Забор у отправителя.</summary>
    SenderPickup = 1,

    /// <summary>Склад / транзитный пункт (хаб).</summary>
    Hub = 2,

    /// <summary>Вручение получателю.</summary>
    RecipientDelivery = 3
}

public enum OrderRouteStopStatus : byte
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}
