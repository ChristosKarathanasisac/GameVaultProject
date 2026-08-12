using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.Orders;
using GameVault.Order.Domain.Enums;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Application.PayOrder;

public sealed class PayOrderHandler : IPayOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICatalogClient _catalogClient;
    private readonly IOrderCompensationService _compensationService;
    private readonly ILogger<PayOrderHandler> _logger;

    public PayOrderHandler(
        IOrderRepository orderRepository,
        IPaymentGateway paymentGateway,
        ICatalogClient catalogClient,
        IOrderCompensationService compensationService,
        ILogger<PayOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _paymentGateway = paymentGateway;
        _catalogClient = catalogClient;
        _compensationService = compensationService;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> HandleAsync(
        PayOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        if (order is null || order.CustomerId != command.CallerId)
            return OrderErrors.OrderNotFound;

        if (order.Status != OrderStatus.Reserved)
            return OrderErrors.InvalidPaymentState;

        var paymentResult = await _paymentGateway.ChargeAsync(order.Id, order.TotalAmount, cancellationToken);

        if (paymentResult.IsSuccess)
        {
            var allConfirmed = await ConfirmAllReservationsAsync(order, cancellationToken);
            if (allConfirmed)
            {
                order.MarkPaid();
                await _orderRepository.SaveChangesAsync(cancellationToken);
                return order.ToOrderResponse();
            }
        }

        return await CompensateAndFailAsync(order, cancellationToken);
    }

    private async Task<bool> ConfirmAllReservationsAsync(OrderEntity order, CancellationToken cancellationToken)
    {
        foreach (var line in order.Lines)
        {
            var result = await _catalogClient.ConfirmReservationAsync(order.Id, line.ProductId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Catalog/Order reservation disagreement for order {OrderId}, product {ProductId} — confirm returned non-success; falling back to release",
                    order.Id, line.ProductId);
                return false;
            }
        }

        return true;
    }

    private async Task<Result<OrderResponse>> CompensateAndFailAsync(
        OrderEntity order,
        CancellationToken cancellationToken)
    {
        var productIds = order.Lines.Select(l => l.ProductId).ToList();
        var allReleased = await _compensationService.ReleaseReservationsAsync(order.Id, productIds, cancellationToken);

        if (!allReleased)
        {
            order.MarkCompensationFailed();
            _logger.LogError(
                "Order {OrderId} entered CompensationFailed status after payment failure — one or more reservations could not be released",
                order.Id);
        }
        else
        {
            order.MarkPaymentFailed();
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);
        return OrderErrors.PaymentDeclined;
    }
}
