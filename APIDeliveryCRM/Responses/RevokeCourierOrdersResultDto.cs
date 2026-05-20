namespace APIDeliveryCRM.Responses;

public class RevokeCourierOrdersResultDto
{
    public int RevokedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
