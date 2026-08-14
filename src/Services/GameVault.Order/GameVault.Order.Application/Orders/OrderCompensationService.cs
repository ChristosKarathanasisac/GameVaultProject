using GameVault.Order.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameVault.Order.Application.Orders;

public sealed class OrderCompensationService : IOrderCompensationService
{
    private const int MaxReleaseRetries = 3;

    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<OrderCompensationService> _logger;

    public OrderCompensationService(ICatalogClient catalogClient, ILogger<OrderCompensationService> logger)
    {
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<bool> ReleaseReservationsAsync(
        Guid orderId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var anyReleaseFailed = false;

        foreach (var productId in productIds)
        {
            var released = await TryReleaseWithRetryAsync(orderId, productId, cancellationToken);
            if (!released)
            {
                anyReleaseFailed = true;
                _logger.LogError(
                    "Failed to release reservation for order {OrderId}, product {ProductId} after {MaxRetries} attempts — manual reconciliation may be required",
                    orderId, productId, MaxReleaseRetries);
            }
        }

        return !anyReleaseFailed;
    }

    private async Task<bool> TryReleaseWithRetryAsync(
        Guid orderId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxReleaseRetries; attempt++)
        {
            var result = await _catalogClient.ReleaseReservationAsync(orderId, productId, cancellationToken);
            if (result.IsSuccess)
                return true;
        }

        return false;
    }
}
