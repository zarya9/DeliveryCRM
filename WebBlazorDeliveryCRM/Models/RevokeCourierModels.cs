namespace WebBlazorDeliveryCRM.Models;

public class RevokeCourierOrdersRequestDto
{
    public int CourierId { get; set; }
    public List<int>? OrderIds { get; set; }
    public string? Reason { get; set; }
}

public class RevokeCourierOrdersResultDto
{
    public int RevokedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
