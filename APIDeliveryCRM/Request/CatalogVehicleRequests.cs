using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class CreateCatalogBrandRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; } = "";
    }

    public class CreateCatalogModelRequest
    {
        [Range(1, int.MaxValue)]
        public int BrandId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = "";

        /// <summary>Год модели в справочнике (по умолчанию на сервере — текущий календарный год).</summary>
        [Range(1980, 2100)]
        public int? Year { get; set; }
    }
}
