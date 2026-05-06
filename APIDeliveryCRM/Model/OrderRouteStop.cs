using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model;

public class OrderRouteStop
{
    [Key]
    public int ID_OrderRouteStop { get; set; }

    [Required]
    [ForeignKey(nameof(Order))]
    public int Order_id { get; set; }

    [JsonIgnore]
    public Order Order { get; set; } = null!;

    public int SortOrder { get; set; }

    public OrderRouteStopKind Kind { get; set; }

    public OrderRouteStopStatus Status { get; set; } = OrderRouteStopStatus.Pending;

    [MaxLength(500)]
    public string? Title { get; set; }

    [Required]
    [ForeignKey(nameof(Address))]
    public int Address_id { get; set; }
    public Address Address { get; set; } = null!;

    [ForeignKey(nameof(LogisticsHub))]
    public int? LogisticsHub_id { get; set; }
    public LogisticsHub? LogisticsHub { get; set; }

    [ForeignKey(nameof(AssignedCourier))]
    public int? AssignedCourier_id { get; set; }
    public CourierProfile? AssignedCourier { get; set; }
}
