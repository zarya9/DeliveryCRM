using System;
using System.Collections.Generic;

namespace WebBlazorDeliveryCRM.Models
{
    public class ClientDetailsDto
    {
        public int ClientProfileId { get; set; }
        public int UserId { get; set; }
        public string FName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Patronumic { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal Rating { get; set; }
        public string? Status { get; set; }
        public string? Segment { get; set; }
        public List<ClientOrderShortDto> Orders { get; set; } = new();
        public List<ClientNoteShortDto> Notes { get; set; } = new();
    }

    public class ClientOrderShortDto
    {
        public int OrderId { get; set; }
        public int OrderNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal FinalCost { get; set; }
    }

    public class ClientNoteShortDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}

