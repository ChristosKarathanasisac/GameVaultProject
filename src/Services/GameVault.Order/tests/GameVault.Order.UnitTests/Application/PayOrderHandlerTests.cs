using GameVault.Contracts.Responses.Catalog;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.PayOrder;
using GameVault.Order.Application.Payments;
using GameVault.Order.Domain.Entities;
using GameVault.Order.Domain.Enums;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.UnitTests.Application;

public sealed class PayOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly ICatalogClient _catalogClient = Substitute.For<ICatalogClient>();
    private readonly IOrderCompensationService _compensationService = Substitute.For<IOrderCompensationService>();
    private readonly ILogger<PayOrderHandler> _logger = Substitute.For<ILogger<PayOrderHandler>>();
    private readonly PayOrderHandler _handler;

    public PayOrderHandlerTests()
    {
        _handler = new PayOrderHandler(_orderRepository, _paymentGateway, _catalogClient, _compensationService, _logger);
    }

    // ── Order not found / ownership ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        _orderRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrderEntity?)null);

        var result = await _handler.HandleAsync(new PayOrderCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenOrderBelongsToAnotherCustomer()
    {
        var order = CreateReservedOrder(Guid.NewGuid());
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, CallerId: Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
        await _paymentGateway.DidNotReceiveWithAnyArgs().ChargeAsync(default, default, default);
    }

    // ── Invalid status ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsInvalidPaymentState_WhenOrderIsNotReserved()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePendingOrder(customerId);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidPaymentState, result.Error);
        await _paymentGateway.DidNotReceiveWithAnyArgs().ChargeAsync(default, default, default);
    }

    // ── Payment succeeds, all confirms succeed → Paid ─────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsOrderWithPaidStatus_WhenPaymentAndConfirmsSucceed()
    {
        var customerId = Guid.NewGuid();
        var (order, productId) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        SetupSuccessfulPayment(order.Id);
        SetupSuccessfulConfirm(order.Id, productId);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid.ToString(), result.Value.Status);
    }

    [Fact]
    public async Task HandleAsync_NeverCallsCompensation_WhenPaymentAndConfirmsSucceed()
    {
        var customerId = Guid.NewGuid();
        var (order, productId) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        SetupSuccessfulPayment(order.Id);
        SetupSuccessfulConfirm(order.Id, productId);

        await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        await _compensationService.DidNotReceiveWithAnyArgs()
            .ReleaseReservationsAsync(default, default!, default);
    }

    // ── Payment succeeds, a confirm fails → compensation → PaymentFailed ──────

    [Fact]
    public async Task HandleAsync_ReturnsPaymentDeclined_WhenConfirmFails_AndAllReleasesSucceed()
    {
        var customerId = Guid.NewGuid();
        var (order, productId) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        SetupSuccessfulPayment(order.Id);
        _catalogClient
            .ConfirmReservationAsync(order.Id, productId, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(Error.Conflict("Catalog.ConfirmFailed", "not found")));
        _compensationService
            .ReleaseReservationsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PaymentDeclined, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaymentDeclined_WhenConfirmFails_AndAReleaseFails()
    {
        var customerId = Guid.NewGuid();
        var (order, productId) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        SetupSuccessfulPayment(order.Id);
        _catalogClient
            .ConfirmReservationAsync(order.Id, productId, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(Error.Conflict("Catalog.ConfirmFailed", "not found")));
        _compensationService
            .ReleaseReservationsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PaymentDeclined, result.Error);
    }

    // ── Payment declined → compensation → PaymentFailed or CompensationFailed ─

    [Fact]
    public async Task HandleAsync_ReturnsPaymentDeclined_WhenPaymentDeclined_AndAllReleasesSucceed()
    {
        var customerId = Guid.NewGuid();
        var (order, _) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        _paymentGateway
            .ChargeAsync(order.Id, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Failure(OrderErrors.PaymentDeclined));
        _compensationService
            .ReleaseReservationsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PaymentDeclined, result.Error);
        await _catalogClient.DidNotReceiveWithAnyArgs()
            .ConfirmReservationAsync(default, default, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaymentDeclined_WhenPaymentDeclined_AndAReleaseFails()
    {
        var customerId = Guid.NewGuid();
        var (order, _) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        _paymentGateway
            .ChargeAsync(order.Id, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Failure(OrderErrors.PaymentDeclined));
        _compensationService
            .ReleaseReservationsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PaymentDeclined, result.Error);
    }

    [Fact]
    public async Task HandleAsync_CallsCompensation_WithAllOrderLineProductIds_WhenPaymentDeclined()
    {
        var customerId = Guid.NewGuid();
        var (order, productId) = CreateReservedOrderWithLine(customerId);
        SetupOrderLoad(order);
        _paymentGateway
            .ChargeAsync(order.Id, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Failure(OrderErrors.PaymentDeclined));
        _compensationService
            .ReleaseReservationsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.HandleAsync(new PayOrderCommand(order.Id, customerId));

        await _compensationService.Received(1).ReleaseReservationsAsync(
            order.Id,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(productId)),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrderEntity CreatePendingOrder(Guid customerId)
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Test Game", 29.99m, 1);
        return OrderEntity.Create(customerId, [line]);
    }

    private static OrderEntity CreateReservedOrder(Guid customerId)
    {
        var order = CreatePendingOrder(customerId);
        order.MarkReserved();
        return order;
    }

    private static (OrderEntity Order, Guid ProductId) CreateReservedOrderWithLine(Guid customerId)
    {
        var productId = Guid.NewGuid();
        var line = OrderLine.Create(productId, "Test Game", 29.99m, 1);
        var order = OrderEntity.Create(customerId, [line]);
        order.MarkReserved();
        return (order, productId);
    }

    private void SetupOrderLoad(OrderEntity order)
    {
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
    }

    private void SetupSuccessfulPayment(Guid orderId)
    {
        _paymentGateway
            .ChargeAsync(orderId, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Success(new PaymentResult(orderId)));
    }

    private void SetupSuccessfulConfirm(Guid orderId, Guid productId)
    {
        _catalogClient
            .ConfirmReservationAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));
    }
}
