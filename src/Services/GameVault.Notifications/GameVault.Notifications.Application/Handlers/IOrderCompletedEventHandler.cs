using GameVault.Contracts.Events.Order;

namespace GameVault.Notifications.Application.Handlers;

public interface IOrderCompletedEventHandler
{
    Task HandleAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default);
}
