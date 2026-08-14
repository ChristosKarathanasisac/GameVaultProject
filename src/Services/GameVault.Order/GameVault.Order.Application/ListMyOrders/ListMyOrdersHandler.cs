using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Orders;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.ListMyOrders;

public sealed class ListMyOrdersHandler : IListMyOrdersHandler
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IOrderRepository _orderRepository;

    public ListMyOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResponse<OrderResponse>>> HandleAsync(
        ListMyOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? DefaultPageSize
            : query.PageSize > MaxPageSize ? MaxPageSize
            : query.PageSize;

        var (items, totalCount) = await _orderRepository.ListByCustomerAsync(
            query.CustomerId, page, pageSize, cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<OrderResponse>(
            items.Select(o => o.ToOrderResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            totalPages);
    }
}
