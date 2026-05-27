using System.Collections.Generic;

namespace APIDeliveryCRM.Request
{
    public class SendMessageRequest
    {
        public string? MessageText { get; set; }
        public string? AttachmentUrl { get; set; }

        //ID сообщения, на которое отвечаем.
        public int? ReplyToMessageId { get; set; }

        //Список ID пользователей, упомянутых через @сабачка.
        public List<int>? MentionedUserIds { get; set; }
    }
}
