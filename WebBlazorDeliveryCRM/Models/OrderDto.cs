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
    public bool Is_paid { get; set; }
    public OrderStatusDto? OrderStatus { get; set; }
    public ClientProfileDto? ClientProfile { get; set; }
    public CourierProfileDto? CourierProfile { get; set; }
    public OrderTypeDto? OrderType { get; set; }
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
