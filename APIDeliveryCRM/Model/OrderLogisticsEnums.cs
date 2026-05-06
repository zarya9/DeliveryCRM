namespace APIDeliveryCRM.Model;

public enum DeliveryRouteKind : byte
{
    LocalUrban = 1,

    ViaHub = 2,

    DirectIntercity = 3
}

public enum OrderRouteStopKind : byte
{
    SenderPickup = 1,

    Hub = 2,

    RecipientDelivery = 3
}

public enum OrderRouteStopStatus : byte
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}
