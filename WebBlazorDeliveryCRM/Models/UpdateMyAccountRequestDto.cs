namespace WebBlazorDeliveryCRM.Models;

public sealed class UpdateMyAccountRequestDto
{
    public string? FName { get; set; }
    public string? Name { get; set; }
    public string? Patronumic { get; set; }
    public string? NewEmail { get; set; }
    public string? NewPassword { get; set; }
    public string? CurrentPassword { get; set; }
}
