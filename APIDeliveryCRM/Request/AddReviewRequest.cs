namespace APIDeliveryCRM.Request
{
    public class AddReviewRequest
    {
        public int OrderId { get; set; }
        public int AuthorId { get; set; }
        public int TargetUserId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}


