using GameVault.Contracts.Responses.Order;
using GameVault.Order.Domain.Entities;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Application.Orders;

internal static class OrderMapper
{
    internal static OrderResponse ToOrderResponse(this OrderEntity order) =>
        new(order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.Lines.Select(l => l.ToOrderLineResponse()).ToList());

    private static OrderLineResponse ToOrderLineResponse(this OrderLine line) =>
        new(line.ProductId, line.ProductTitle, line.UnitPrice, line.Quantity);
}
