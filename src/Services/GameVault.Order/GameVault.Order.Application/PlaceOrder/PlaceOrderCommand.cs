namespace GameVault.Order.Application.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    IReadOnlyList<PlaceOrderLineCommand> Lines);

public sealed record PlaceOrderLineCommand(
    Guid ProductId,
    int Quantity);
