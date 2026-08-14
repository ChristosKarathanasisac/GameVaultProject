using GameVault.Catalog.Application.Reservations.ConfirmReservation;
using GameVault.Catalog.Application.Reservations.ReleaseReservation;
using GameVault.Catalog.Application.Reservations.ReserveStock;
using GameVault.Contracts.Requests.Catalog;
using GameVault.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Catalog.Api.Controllers;

[ApiController]
public class ReservationsController : ControllerBase
{
    private readonly IReserveStockHandler _reserveStockHandler;
    private readonly IConfirmReservationHandler _confirmReservationHandler;
    private readonly IReleaseReservationHandler _releaseReservationHandler;

    public ReservationsController(
        IReserveStockHandler reserveStockHandler,
        IConfirmReservationHandler confirmReservationHandler,
        IReleaseReservationHandler releaseReservationHandler)
    {
        _reserveStockHandler = reserveStockHandler;
        _confirmReservationHandler = confirmReservationHandler;
        _releaseReservationHandler = releaseReservationHandler;
    }

    [HttpPost("api/products/{productId:guid}/reservations")]
    public async Task<IActionResult> Reserve(Guid productId, [FromBody] ReserveStockRequest request, CancellationToken cancellationToken)
    {
        var command = new ReserveStockCommand(productId, request.OrderId, request.Quantity);
        var result = await _reserveStockHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpPost("api/orders/{orderId:guid}/products/{productId:guid}/reservations/confirm")]
    public async Task<IActionResult> Confirm(Guid orderId, Guid productId, CancellationToken cancellationToken)
    {
        var command = new ConfirmReservationCommand(orderId, productId);
        var result = await _confirmReservationHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpPost("api/orders/{orderId:guid}/products/{productId:guid}/reservations/release")]
    public async Task<IActionResult> Release(Guid orderId, Guid productId, CancellationToken cancellationToken)
    {
        var command = new ReleaseReservationCommand(orderId, productId);
        var result = await _releaseReservationHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(Ok);
    }
}
