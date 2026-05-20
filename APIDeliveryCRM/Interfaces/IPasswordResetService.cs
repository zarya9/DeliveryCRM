using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces;

public interface IPasswordResetService
{
    Task<IActionResult> RequestResetCodeAsync(RequestPasswordResetRequest dto, CancellationToken cancellationToken = default);

    Task<IActionResult> CompleteResetAsync(CompletePasswordResetRequest dto, CancellationToken cancellationToken = default);
}
