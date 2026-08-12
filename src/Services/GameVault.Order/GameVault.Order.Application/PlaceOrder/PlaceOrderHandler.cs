using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.Orders;
using GameVault.Order.Domain.Entities;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Application.PlaceOrder;

public sealed class PlaceOrderHandler : IPlaceOrderHandler
{
    private const int MaxReleaseRetries = 3;

    private readonly IOrderRepository _orderRepository;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<PlaceOrderHandler> _logger;

    public PlaceOrderHandler(
        IOrderRepository orderRepository,
        ICatalogClient catalogClient,
        ILogger<PlaceOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = OrderRequestValidation.ValidateLines(command.Lines);
        if (validationError is not null)
            return validationError;

        var lineData = new List<(PlaceOrderLineCommand Line, Guid ProductId, string ProductTitle, decimal UnitPrice)>();
        foreach (var line in command.Lines)
        {
            var productResult = await _catalogClient.GetProductAsync(line.ProductId, cancellationToken);
            if (productResult.IsFailure)
                return OrderErrors.ProductNotFound;

            var product = productResult.Value;
            lineData.Add((line, product.Id, product.Title, product.Price));
        }

        var orderLines = lineData.Select(ld =>
            OrderLine.Create(ld.ProductId, ld.ProductTitle, ld.UnitPrice, ld.Line.Quantity)).ToList();

        var order = OrderEntity.Create(command.CustomerId, orderLines);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var reservedProductIds = new List<Guid>();

        foreach (var (line, productId, _, _) in lineData)
        {
            var reserveResult = await _catalogClient.ReserveStockAsync(
                productId, order.Id, line.Quantity, cancellationToken);

            if (reserveResult.IsSuccess)
            {
                reservedProductIds.Add(productId);
            }
            else
            {
                break;
            }
        }

        if (reservedProductIds.Count == lineData.Count)
        {
            order.MarkReserved();
            await _orderRepository.SaveChangesAsync(cancellationToken);
            return order.ToOrderResponse();
        }

        var anyReleaseFailed = false;
        foreach (var productId in reservedProductIds)
        {
            var released = await TryReleaseWithRetryAsync(order.Id, productId, cancellationToken);
            if (!released)
            {
                anyReleaseFailed = true;
                _logger.LogError(
                    "Failed to release reservation for order {OrderId}, product {ProductId} after {MaxRetries} attempts — manual reconciliation may be required",
                    order.Id, productId, MaxReleaseRetries);
            }
        }

        if (anyReleaseFailed)
        {
            order.MarkCompensationFailed();
            _logger.LogError(
                "Order {OrderId} entered CompensationFailed status — one or more reservations could not be released",
                order.Id);
        }
        else
        {
            order.MarkFailed();
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);
        return OrderErrors.PlacementFailed;
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
