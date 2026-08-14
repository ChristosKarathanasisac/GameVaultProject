using Dapr;
using GameVault.Contracts.Events.Order;
using GameVault.Notifications.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Notifications.Api.Controllers;

[ApiController]
[Route("notifications")]
public sealed class NotificationsController : ControllerBase
{
    private const string PubSubName = "gamevault-pubsub";
    private const string OrderCompletedTopic = "order-completed";

    private readonly IOrderCompletedEventHandler _handler;

    public NotificationsController(IOrderCompletedEventHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("order-completed")]
    [Topic(PubSubName, OrderCompletedTopic)]
    public async Task<IActionResult> HandleOrderCompleted(
        OrderCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(@event, cancellationToken);
        return Ok();
    }
}
