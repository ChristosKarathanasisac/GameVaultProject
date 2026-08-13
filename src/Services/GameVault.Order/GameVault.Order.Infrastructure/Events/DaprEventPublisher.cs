using System.Net.Http.Json;
using GameVault.Contracts.Events.Order;
using GameVault.Order.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameVault.Order.Infrastructure.Events;

public sealed class DaprEventPublisher : IEventPublisher
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DaprEventPublisher> _logger;

    public DaprEventPublisher(IHttpClientFactory httpClientFactory, ILogger<DaprEventPublisher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task PublishOrderCompletedAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        var url = $"/v1.0/publish/{EventPublisherConsts.PubSubName}/{EventPublisherConsts.OrderCompletedTopic}";

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(EventPublisherConsts.DaprHttpClientName);
                var response = await client.PostAsJsonAsync(url, @event, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return;

                _logger.LogWarning(
                    "Dapr pub/sub publish returned {StatusCode} for order {OrderId} (attempt {Attempt}/{MaxRetries})",
                    (int)response.StatusCode, @event.OrderId, attempt, MaxRetries);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex,
                    "Dapr pub/sub publish failed for order {OrderId} (attempt {Attempt}/{MaxRetries})",
                    @event.OrderId, attempt, MaxRetries);
            }

            await Task.Delay(RetryDelay, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Failed to publish OrderCompletedEvent for order {@event.OrderId} after {MaxRetries} attempts.");
    }
}
