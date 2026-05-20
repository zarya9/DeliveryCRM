namespace APIDeliveryCRM.Request;

public class RevokeCourierOrdersRequest
{
    public int CourierId { get; set; }
    public List<int>? OrderIds { get; set; }
    public string? Reason { get; set; }
}
