namespace APIDeliveryCRM.Model
{
    public enum SupportTicketCategory : byte
    {
        Complaint = 1,
        Return = 2,
        LostItem = 3,
        Other = 4
    }

    public enum SupportTicketStatus : byte
    {
        New = 1,
        InProgress = 2,
        WaitingClient = 3,
        Resolved = 4,
        Closed = 5
    }
}
