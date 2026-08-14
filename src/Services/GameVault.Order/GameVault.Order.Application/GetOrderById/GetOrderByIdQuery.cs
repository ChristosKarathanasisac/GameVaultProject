namespace GameVault.Order.Application.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid CallerId);
