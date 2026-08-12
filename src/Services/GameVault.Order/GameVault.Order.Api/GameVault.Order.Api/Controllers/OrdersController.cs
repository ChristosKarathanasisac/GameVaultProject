using GameVault.Contracts.Requests.Order;
using GameVault.Core.Extensions;
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

    public OrdersController(IPlaceOrderHandler placeOrderHandler)
    {
        _placeOrderHandler = placeOrderHandler;
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
}
