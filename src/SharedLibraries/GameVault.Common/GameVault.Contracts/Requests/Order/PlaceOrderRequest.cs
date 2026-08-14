namespace GameVault.Contracts.Requests.Order;

public sealed record PlaceOrderRequest(IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);
