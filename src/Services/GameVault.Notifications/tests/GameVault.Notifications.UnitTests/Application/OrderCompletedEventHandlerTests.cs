using GameVault.Contracts.Events.Order;
using GameVault.Notifications.Application.Handlers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameVault.Notifications.UnitTests.Application;

public sealed class OrderCompletedEventHandlerTests
{
    private readonly ILogger<OrderCompletedEventHandler> _logger =
        Substitute.For<ILogger<OrderCompletedEventHandler>>();

    private readonly OrderCompletedEventHandler _handler;

    public OrderCompletedEventHandlerTests()
    {
        _handler = new OrderCompletedEventHandler(_logger);
    }

    [Fact]
    public async Task HandleAsync_CompletesSuccessfully_ForValidEvent()
    {
        var @event = new OrderCompletedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            TotalAmount: 59.99m,
            PaidAtUtc: DateTime.UtcNow);

        await _handler.HandleAsync(@event);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains(@event.OrderId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_LogsOrderId_InMessage()
    {
        var orderId = Guid.NewGuid();
        var @event = new OrderCompletedEvent(
            OrderId: orderId,
            CustomerId: Guid.NewGuid(),
            TotalAmount: 29.99m,
            PaidAtUtc: DateTime.UtcNow);

        await _handler.HandleAsync(@event);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains(orderId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsCompletedTask()
    {
        var @event = new OrderCompletedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            TotalAmount: 9.99m,
            PaidAtUtc: DateTime.UtcNow);

        var task = _handler.HandleAsync(@event);

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }
}
