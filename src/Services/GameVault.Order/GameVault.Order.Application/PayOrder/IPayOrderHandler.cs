using GameVault.Contracts.Responses.Order;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.PayOrder;

public interface IPayOrderHandler
{
    Task<Result<OrderResponse>> HandleAsync(PayOrderCommand command, CancellationToken cancellationToken = default);
}
