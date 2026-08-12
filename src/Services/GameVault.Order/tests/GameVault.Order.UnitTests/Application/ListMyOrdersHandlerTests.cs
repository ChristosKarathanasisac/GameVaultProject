using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.ListMyOrders;
using GameVault.Order.Domain.Entities;
using NSubstitute;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.UnitTests.Application;

public sealed class ListMyOrdersHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ListMyOrdersHandler _handler;

    public ListMyOrdersHandlerTests()
    {
        _handler = new ListMyOrdersHandler(_orderRepository);
    }

    // ── Empty list ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsEmptyPagedResponse_WhenCustomerHasNoOrders()
    {
        var customerId = Guid.NewGuid();
        _orderRepository
            .ListByCustomerAsync(customerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderEntity>(), 0));

        var result = await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    // ── Populated list ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsMappedOrders_WhenCustomerHasOrders()
    {
        var customerId = Guid.NewGuid();
        var orders = new[] { CreateOrder(customerId), CreateOrder(customerId) };
        _orderRepository
            .ListByCustomerAsync(customerId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((orders, 2));

        var result = await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_CalculatesTotalPagesCorrectly_WhenCountExceedsPageSize()
    {
        var customerId = Guid.NewGuid();
        var orders = Enumerable.Range(0, 20).Select(_ => CreateOrder(customerId)).ToArray();
        _orderRepository
            .ListByCustomerAsync(customerId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((orders, 45));

        var result = await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
    }

    // ── Page/size normalization ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NormalizesPageToOne_WhenPageIsZeroOrNegative()
    {
        var customerId = Guid.NewGuid();
        _orderRepository
            .ListByCustomerAsync(customerId, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderEntity>(), 0));

        await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 0, 20));

        await _orderRepository.Received(1).ListByCustomerAsync(customerId, 1, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NormalizesPageSizeToDefault_WhenPageSizeIsZeroOrNegative()
    {
        var customerId = Guid.NewGuid();
        _orderRepository
            .ListByCustomerAsync(customerId, Arg.Any<int>(), 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderEntity>(), 0));

        await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 1, 0));

        await _orderRepository.Received(1).ListByCustomerAsync(customerId, Arg.Any<int>(), 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSizeToMax_WhenPageSizeExceedsMaximum()
    {
        var customerId = Guid.NewGuid();
        _orderRepository
            .ListByCustomerAsync(customerId, Arg.Any<int>(), 100, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderEntity>(), 0));

        await _handler.HandleAsync(new ListMyOrdersQuery(customerId, 1, 9999));

        await _orderRepository.Received(1).ListByCustomerAsync(customerId, Arg.Any<int>(), 100, Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrderEntity CreateOrder(Guid customerId)
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Test Game", 29.99m, 1);
        return OrderEntity.Create(customerId, [line]);
    }
}
