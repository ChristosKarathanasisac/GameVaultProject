using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.Orders;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.GetOrderById;

public sealed class GetOrderByIdHandler : IGetOrderByIdHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderResponse>> HandleAsync(
        GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order is null || order.CustomerId != query.CallerId)
            return OrderErrors.OrderNotFound;

        return order.ToOrderResponse();
    }
}
