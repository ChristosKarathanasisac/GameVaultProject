using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.GetOrderById;
using GameVault.Order.Domain.Entities;
using GameVault.Order.Domain.Enums;
using NSubstitute;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.UnitTests.Application;

public sealed class GetOrderByIdHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly GetOrderByIdHandler _handler;

    public GetOrderByIdHandlerTests()
    {
        _handler = new GetOrderByIdHandler(_orderRepository);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        _orderRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrderEntity?)null);

        var result = await _handler.HandleAsync(new GetOrderByIdQuery(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenOrderBelongsToAnotherCustomer()
    {
        var order = CreateOrder(Guid.NewGuid());
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.HandleAsync(new GetOrderByIdQuery(order.Id, CallerId: Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsOrderResponse_WhenCallerOwnsTheOrder()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(customerId);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.HandleAsync(new GetOrderByIdQuery(order.Id, customerId));

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Value.Id);
        Assert.Equal(OrderStatus.Pending.ToString(), result.Value.Status);
    }

    // ── CompensationFailed status mapping ─────────────────────────────────────

    [Fact]
    public async Task HandleAsync_MapsCompensationFailed_ToFailed_WhenPriorStatusWasPending()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(customerId);
        order.MarkCompensationFailed();
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.HandleAsync(new GetOrderByIdQuery(order.Id, customerId));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Failed.ToString(), result.Value.Status);
    }

    [Fact]
    public async Task HandleAsync_MapsCompensationFailed_ToPaymentFailed_WhenPriorStatusWasReserved()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(customerId);
        order.MarkReserved();
        order.MarkCompensationFailed();
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.HandleAsync(new GetOrderByIdQuery(order.Id, customerId));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.PaymentFailed.ToString(), result.Value.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrderEntity CreateOrder(Guid customerId)
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Test Game", 29.99m, 1);
        return OrderEntity.Create(customerId, [line]);
    }
}
