using GameVault.Contracts.Requests.Order;
using GameVault.Core.Extensions;
using GameVault.Order.Application.GetOrderById;
using GameVault.Order.Application.ListMyOrders;
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
    private readonly IListMyOrdersHandler _listMyOrdersHandler;
    private readonly IGetOrderByIdHandler _getOrderByIdHandler;

    public OrdersController(
        IPlaceOrderHandler placeOrderHandler,
        IPayOrderHandler payOrderHandler,
        IListMyOrdersHandler listMyOrdersHandler,
        IGetOrderByIdHandler getOrderByIdHandler)
    {
        _placeOrderHandler = placeOrderHandler;
        _payOrderHandler = payOrderHandler;
        _listMyOrdersHandler = listMyOrdersHandler;
        _getOrderByIdHandler = getOrderByIdHandler;
    }

    [HttpGet]
    public async Task<IActionResult> ListMyOrders(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();
        if (customerId is null)
            return Unauthorized();

        var query = new ListMyOrdersQuery(customerId.Value, page, pageSize);
        var result = await _listMyOrdersHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
            return Unauthorized();

        var query = new GetOrderByIdQuery(orderId, callerId.Value);
        var result = await _getOrderByIdHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(Ok);
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
