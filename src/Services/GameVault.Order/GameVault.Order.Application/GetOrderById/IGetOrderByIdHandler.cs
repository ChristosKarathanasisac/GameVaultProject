using GameVault.Contracts.Responses.Order;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.GetOrderById;

public interface IGetOrderByIdHandler
{
    Task<Result<OrderResponse>> HandleAsync(
        GetOrderByIdQuery query, CancellationToken cancellationToken = default);
}
