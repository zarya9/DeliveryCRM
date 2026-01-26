namespace APIDeliveryCRM.Request
{
    public class EditReviewRequest
    {
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}


