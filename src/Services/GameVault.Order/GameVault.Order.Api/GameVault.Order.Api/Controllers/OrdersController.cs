using GameVault.Contracts.Requests.Order;
using GameVault.Core.Extensions;
using GameVault.Order.Application.PayOrder;
using GameVault.Order.Application.PlaceOrder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Order.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IPlaceOrderHandler _placeOrderHandler;
    private readonly IPayOrderHandler _payOrderHandler;

    public OrdersController(IPlaceOrderHandler placeOrderHandler, IPayOrderHandler payOrderHandler)
    {
        _placeOrderHandler = placeOrderHandler;
        _payOrderHandler = payOrderHandler;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        if (customerId is null)
            return Unauthorized();

        var command = new PlaceOrderCommand(
            customerId.Value,
            request.Lines.Select(l => new PlaceOrderLineCommand(l.ProductId, l.Quantity)).ToList());

        var result = await _placeOrderHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpPost("{orderId:guid}/pay")]
    public async Task<IActionResult> PayOrder(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
            return Unauthorized();

        var command = new PayOrderCommand(orderId, callerId.Value);

        var result = await _payOrderHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(Ok);
    }
}
