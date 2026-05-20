namespace APIDeliveryCRM.Responses;

public class RouteStopCompletionResultDto
{
    public int AssignmentId { get; set; }
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public int? NewStatusId { get; set; }
    public string? NewStatusName { get; set; }
    public bool OrderDelivered { get; set; }
    public bool HubHandoffTriggered { get; set; }
}
