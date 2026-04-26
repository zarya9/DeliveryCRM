using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model;

/// <summary>Склад / сортировочный пункт компании (хаб маршрута).</summary>
public class LogisticsHub
{
    [Key]
    public int ID_LogisticsHub { get; set; }

    [Required]
    [ForeignKey(nameof(Company))]
    public int Company_id { get; set; }
    public Company Company { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [ForeignKey(nameof(Address))]
    public int Address_id { get; set; }
    public Address Address { get; set; } = null!;
}
