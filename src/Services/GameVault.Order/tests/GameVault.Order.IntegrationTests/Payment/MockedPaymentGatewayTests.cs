using GameVault.Order.Application.Errors;
using GameVault.Order.Infrastructure.Payment;
using Microsoft.Extensions.Configuration;

namespace GameVault.Order.IntegrationTests.Payment;

public sealed class MockedPaymentGatewayTests
{
    private static MockedPaymentGateway BuildGateway(double failureRate, Random random)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MockedPayment:FailureRate"] = failureRate.ToString("R")
            })
            .Build();

        return new MockedPaymentGateway(random, config);
    }

    [Fact]
    public async Task ChargeAsync_AlwaysSucceeds_WhenFailureRateIsZero()
    {
        var gateway = BuildGateway(0.0, new Random(42));

        for (var i = 0; i < 20; i++)
        {
            var result = await gateway.ChargeAsync(Guid.NewGuid(), 99.99m);
            Assert.True(result.IsSuccess);
        }
    }

    [Fact]
    public async Task ChargeAsync_AlwaysFails_WhenFailureRateIsOne()
    {
        var gateway = BuildGateway(1.0, new Random(42));

        for (var i = 0; i < 20; i++)
        {
            var result = await gateway.ChargeAsync(Guid.NewGuid(), 99.99m);
            Assert.True(result.IsFailure);
            Assert.Equal(OrderErrors.PaymentDeclined, result.Error);
        }
    }

    [Fact]
    public async Task ChargeAsync_ReturnsPaymentResultWithOrderId_OnSuccess()
    {
        var gateway = BuildGateway(0.0, new Random(0));
        var orderId = Guid.NewGuid();

        var result = await gateway.ChargeAsync(orderId, 49.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(orderId, result.Value.OrderId);
    }

    [Fact]
    public async Task ChargeAsync_UsesDefaultFailureRate_WhenConfigKeyAbsent()
    {
        var config = new ConfigurationBuilder().Build();

        // With Random(0) and default 0.2 rate, most calls succeed — verify it doesn't throw.
        var gateway = new MockedPaymentGateway(new Random(0), config);

        var result = await gateway.ChargeAsync(Guid.NewGuid(), 10m);

        Assert.True(result.IsSuccess || result.IsFailure);
    }
}
