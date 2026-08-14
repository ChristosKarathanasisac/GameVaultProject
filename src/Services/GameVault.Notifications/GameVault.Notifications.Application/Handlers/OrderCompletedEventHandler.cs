using GameVault.Contracts.Events.Order;
using Microsoft.Extensions.Logging;

namespace GameVault.Notifications.Application.Handlers;

public sealed class OrderCompletedEventHandler : IOrderCompletedEventHandler
{
    private readonly ILogger<OrderCompletedEventHandler> _logger;

    public OrderCompletedEventHandler(ILogger<OrderCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Notification received: order {OrderId} for customer {CustomerId} completed, total {TotalAmount}",
            @event.OrderId, @event.CustomerId, @event.TotalAmount);

        return Task.CompletedTask;
    }
}
