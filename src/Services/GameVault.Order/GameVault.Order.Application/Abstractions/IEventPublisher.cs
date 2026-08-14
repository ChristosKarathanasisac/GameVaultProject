using GameVault.Contracts.Events.Order;

namespace GameVault.Order.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishOrderCompletedAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default);
}
