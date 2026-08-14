using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.Payments;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Configuration;

namespace GameVault.Order.Infrastructure.Payment;

// Intentionally fake — no real payment provider integration exists or is planned
// for Phase 1 (BR line 6 in TargetState.md, "mocked only, by design").
public sealed class MockedPaymentGateway : IPaymentGateway
{
    private readonly Random _random;
    private readonly double _failureRate;

    public MockedPaymentGateway(Random random, IConfiguration configuration)
    {
        _random = random;
        _failureRate = configuration.GetValue(PaymentConsts.FailureRateConfigKey, PaymentConsts.DefaultFailureRate);
    }

    public Task<Result<PaymentResult>> ChargeAsync(
        Guid orderId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (_random.NextDouble() < _failureRate)
            return Task.FromResult(Result<PaymentResult>.Failure(OrderErrors.PaymentDeclined));

        return Task.FromResult(Result<PaymentResult>.Success(new PaymentResult(orderId)));
    }
}
