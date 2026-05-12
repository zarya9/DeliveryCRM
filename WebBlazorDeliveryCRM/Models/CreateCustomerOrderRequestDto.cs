using System.ComponentModel.DataAnnotations;

namespace WebBlazorDeliveryCRM.Models;

public class CreateCustomerOrderRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required]
    public string PickupStreet { get; set; } = string.Empty;
    [Required]
    public string PickupHouse { get; set; } = string.Empty;
    public string? PickupFlat { get; set; }
    public string? PickupCity { get; set; }
    public string? PickupComment { get; set; }

    public decimal? PickupLatitude { get; set; }
    public decimal? PickupLongitude { get; set; }

    [Required]
    public string DeliveryStreet { get; set; } = string.Empty;
    [Required]
    public string DeliveryHouse { get; set; } = string.Empty;
    public string? DeliveryFlat { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryComment { get; set; }

    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }

    [Range(0.1, 100000)]
    public decimal Weight { get; set; } = 1;
    [Range(0.1, 100000)]
    public decimal Height { get; set; } = 10;
    [Range(0.1, 100000)]
    public decimal Length { get; set; } = 10;
    [Range(0.1, 100000)]
    public decimal Width { get; set; } = 10;

    [Range(0, 3)]
    public byte Priority { get; set; } = 0;

    /// <summary>Компания-исполнитель (0 — по умолчанию компания учётной записи на сервере).</summary>
    public int CompanyId { get; set; }
}
