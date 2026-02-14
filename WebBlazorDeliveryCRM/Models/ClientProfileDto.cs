namespace WebBlazorDeliveryCRM.Models;

public class ClientProfileDto
{
    public int ID_ClientProfile { get; set; }
    public int User_id { get; set; }
    public string Default_address { get; set; } = "";
    public decimal Rating { get; set; }
    public int Preferred_payment_method_id { get; set; }
    public UserDto? User { get; set; }
}
