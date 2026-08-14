namespace GameVault.Order.Application.PayOrder;

public sealed record PayOrderCommand(Guid OrderId, Guid CallerId);
