using GameVault.Contracts.Responses.Order;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.PlaceOrder;

public interface IPlaceOrderHandler
{
    Task<Result<OrderResponse>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default);
}
