using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Responses;

public enum CustomerOrderCreateOutcome
{
    Ok,
    ClientNotFound,
    CompanyNotFound,
    SubscriptionInactive,
    CatalogNotConfigured,
    PaymentMethodsNotConfigured,
    InvalidOperation
}

public sealed class CustomerOrderCreateResult
{
    public CustomerOrderCreateOutcome Outcome { get; private init; }
    public Order? Order { get; private init; }
    public string? Message { get; private init; }

    public static CustomerOrderCreateResult Success(Order order) =>
        new() { Outcome = CustomerOrderCreateOutcome.Ok, Order = order };

    public static CustomerOrderCreateResult Fail(CustomerOrderCreateOutcome outcome, string message) =>
        new() { Outcome = outcome, Message = message };
}
