using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Events.Order;
using GameVault.Notifications.IntegrationTests.Fixtures;

namespace GameVault.Notifications.IntegrationTests.Notifications;

[Collection(nameof(NotificationsApiCollection))]
public sealed class OrderCompletedEndpointTests
{
    private readonly NotificationsApiFactory _factory;

    public OrderCompletedEndpointTests(NotificationsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostOrderCompleted_Returns200_ForValidEvent()
    {
        var client = _factory.CreateClient();
        var @event = new OrderCompletedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            TotalAmount: 29.99m,
            PaidAtUtc: DateTime.UtcNow);

        var response = await client.PostAsJsonAsync("/notifications/order-completed", @event);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
