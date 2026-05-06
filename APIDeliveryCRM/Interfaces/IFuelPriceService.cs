namespace APIDeliveryCRM.Interfaces;

public interface IFuelPriceService
{
    Task<decimal> GetPriceRubPerLiterAsync(string? fuelTypeName, CancellationToken cancellationToken = default);
}
