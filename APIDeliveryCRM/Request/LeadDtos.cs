using System;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class CreateLeadRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Contact { get; set; }

        [Required]
        public int LeadSourceId { get; set; }

        [Required]
        public int LeadStageId { get; set; }

        public string? Comment { get; set; }
    }

    public class LeadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int? ManagerUserId { get; set; }
        public string? ManagerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Comment { get; set; }
    }
}

