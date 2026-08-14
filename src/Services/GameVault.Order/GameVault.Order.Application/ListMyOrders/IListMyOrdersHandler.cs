using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Order;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.ListMyOrders;

public interface IListMyOrdersHandler
{
    Task<Result<PagedResponse<OrderResponse>>> HandleAsync(
        ListMyOrdersQuery query, CancellationToken cancellationToken = default);
}
