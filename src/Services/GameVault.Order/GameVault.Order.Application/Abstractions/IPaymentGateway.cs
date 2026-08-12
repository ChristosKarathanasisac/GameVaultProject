using GameVault.Order.Application.Payments;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.Abstractions;

public interface IPaymentGateway
{
    Task<Result<PaymentResult>> ChargeAsync(
        Guid orderId,
        decimal amount,
        CancellationToken cancellationToken = default);
}
