using GameVault.Contracts.Responses.Order;
using GameVault.Order.Domain.Entities;
using GameVault.Order.Domain.Enums;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Application.Orders;

internal static class OrderMapper
{
    internal static OrderResponse ToOrderResponse(this OrderEntity order) =>
        new(order.Id,
            order.CustomerId,
            MapStatus(order),
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            order.Lines.Select(l => l.ToOrderLineResponse()).ToList());

    private static OrderLineResponse ToOrderLineResponse(this OrderLine line) =>
        new(line.ProductId, line.ProductTitle, line.UnitPrice, line.Quantity);

    // CompensationFailed is internal-only. Map it to the public-facing status
    // the customer would have seen had compensation succeeded:
    //   Pending  → CompensationFailed means reservation-stage failure  → "Failed"
    //   Reserved → CompensationFailed means payment-stage failure      → "PaymentFailed"
    private static string MapStatus(OrderEntity order) =>
        order.Status == OrderStatus.CompensationFailed
            ? (order.PriorStatus == OrderStatus.Reserved
                ? OrderStatus.PaymentFailed.ToString()
                : OrderStatus.Failed.ToString())
            : order.Status.ToString();
}
