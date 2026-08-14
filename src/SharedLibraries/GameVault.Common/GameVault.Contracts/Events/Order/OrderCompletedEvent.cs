namespace GameVault.Contracts.Events.Order;

public sealed record OrderCompletedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime PaidAtUtc);
