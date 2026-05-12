namespace WebBlazorDeliveryCRM.Models;

public class OrderDto
{
    public int ID_Order { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order_Number { get; set; }
    public int Client_id { get; set; }
    public int Status_id { get; set; }
    public int? Courier_id { get; set; }
    public decimal Estimated_cost { get; set; }
    public decimal Final_cost { get; set; }
    public DateTime Created_at { get; set; }
    public DateTime? Delivered_at { get; set; }
    public DateTime? Pickup_started_at { get; set; }
    public DateTime? In_transit_at { get; set; }
    public DateTime? Arrived_at { get; set; }
    public DateTime? Sla_due_at { get; set; }
    public DateTime? Sla_breached_at { get; set; }
    public DateTime? Eta_at { get; set; }
    public string? Delay_reason { get; set; }
    public byte Priority { get; set; }
    public bool Is_paid { get; set; }

    public string? DeliveryRouteKind { get; set; }

    public int? OriginHub_id { get; set; }
    public int? DestinationHub_id { get; set; }

    public List<OrderRouteStopDto>? RouteStops { get; set; }

    public OrderStatusDto? OrderStatus { get; set; }
    public ClientProfileDto? ClientProfile { get; set; }
    public CourierProfileDto? CourierProfile { get; set; }
    public OrderTypeDto? OrderType { get; set; }
    public AddressShortDto? PickupAddress { get; set; }
    public AddressShortDto? DeliveryAddress { get; set; }
}

public class OrderDispatchDto
{
    public int OrderId { get; set; }
    public int CourierId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int ActiveOrders { get; set; }
    public bool IsSlaRisk { get; set; }
    public DateTime? EtaAt { get; set; }
    public string DecisionReason { get; set; } = string.Empty;
}

public class OrderEtaDto
{
    public int OrderId { get; set; }
    public DateTime? EtaAtUtc { get; set; }
    public DateTime? SlaDueAtUtc { get; set; }
    public DateTime? DeliveryWindowFromUtc { get; set; }
    public DateTime? DeliveryWindowToUtc { get; set; }
    public string? DeliveryWindowText { get; set; }
    public bool IsSlaBreached { get; set; }
    public bool IsSlaRisk { get; set; }
    public string? DelayReason { get; set; }
}

public class OrderTimelineEventDto
{
    public int ID_OrderTimelineEvent { get; set; }
    public int Order_id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Message { get; set; }
    public int? OldStatus_id { get; set; }
    public int? NewStatus_id { get; set; }
    public int? OldCourier_id { get; set; }
    public int? NewCourier_id { get; set; }
    public int? ActorUser_id { get; set; }
    public DateTime Created_at { get; set; }
}

public class OrderRouteStopDto
{
    public int ID_OrderRouteStop { get; set; }
    public int SortOrder { get; set; }
    public string? Kind { get; set; }
    public string? Status { get; set; }
    public string? Title { get; set; }
}

public class OrderStatusDto
{
    public int ID_OrderStatus { get; set; }
    public string Name { get; set; } = "";
}

public class OrderTypeDto
{
    public int ID_OrderType { get; set; }
    public string Name { get; set; } = "";
}

public class OrderStatusOptionDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class AddressShortDto
{
    public int ID_Address { get; set; }
    public string Street { get; set; } = "";
    public string House { get; set; } = "";
    public string? Flat { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Comment { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
