namespace GameVault.Contracts.Responses.Order;

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductTitle,
    decimal UnitPrice,
    int Quantity);
