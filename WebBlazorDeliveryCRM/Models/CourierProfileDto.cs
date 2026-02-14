namespace WebBlazorDeliveryCRM.Models;

public class CourierProfileDto
{
    public int ID_CourierProfile { get; set; }
    public int User_id { get; set; }
    public decimal Rating { get; set; }
    public int Total_deliveries { get; set; }
    public bool Is_online { get; set; }
    public decimal Current_lat { get; set; }
    public decimal Current_lon { get; set; }
    public DateTime LastActivity_at { get; set; }
    public UserDto? User { get; set; }
    public CourierStatusDto? CourierStatus { get; set; }
}

public class CourierStatusDto
{
    public int ID_CourierStatus { get; set; }
    public string Name { get; set; } = "";
}
