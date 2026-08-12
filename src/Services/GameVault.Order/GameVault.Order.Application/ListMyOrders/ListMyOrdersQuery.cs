namespace GameVault.Order.Application.ListMyOrders;

public sealed record ListMyOrdersQuery(Guid CustomerId, int Page, int PageSize);
