using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request;

public class CreateLogisticsHubRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string House { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Flat { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
