using GameVault.Contracts.Responses.Catalog;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Errors;
using GameVault.Order.Application.PlaceOrder;
using GameVault.Order.Domain.Enums;
using GameVault.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ClearExtensions;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.UnitTests.Application;

public sealed class PlaceOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICatalogClient _catalogClient = Substitute.For<ICatalogClient>();
    private readonly ILogger<PlaceOrderHandler> _logger = Substitute.For<ILogger<PlaceOrderHandler>>();
    private readonly PlaceOrderHandler _handler;

    public PlaceOrderHandlerTests()
    {
        _handler = new PlaceOrderHandler(_orderRepository, _catalogClient, _logger);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenLinesAreEmpty()
    {
        var command = new PlaceOrderCommand(Guid.NewGuid(), []);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.EmptyOrderLines, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenAnyQuantityIsZero()
    {
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new PlaceOrderLineCommand(Guid.NewGuid(), 0)]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverCallsCatalog_WhenValidationFails()
    {
        var command = new PlaceOrderCommand(Guid.NewGuid(), []);

        await _handler.HandleAsync(command);

        await _catalogClient.DidNotReceiveWithAnyArgs().GetProductAsync(default, default);
    }

    // ── ProductNotFound ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsProductNotFound_WhenCatalogReturnsNotFound()
    {
        var productId = Guid.NewGuid();
        _catalogClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Failure(
                Error.NotFound("Catalog.ProductNotFound", "not found")));

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderLineCommand(productId, 1)]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverReservesStock_WhenProductNotFound()
    {
        var productId = Guid.NewGuid();
        _catalogClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Failure(
                Error.NotFound("Catalog.ProductNotFound", "not found")));

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderLineCommand(productId, 1)]);

        await _handler.HandleAsync(command);

        await _catalogClient.DidNotReceiveWithAnyArgs()
            .ReserveStockAsync(default, default, default, default);
    }

    [Fact]
    public async Task HandleAsync_NeverPersistsOrder_WhenProductNotFound()
    {
        var productId = Guid.NewGuid();
        _catalogClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Failure(
                Error.NotFound("Catalog.ProductNotFound", "not found")));

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderLineCommand(productId, 1)]);

        await _handler.HandleAsync(command);

        await _orderRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // ── All lines reserve successfully ────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsOrderWithReservedStatus_WhenAllLinesSucceed()
    {
        var (productId, customerId) = (Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(productId);
        SetupSuccessfulReservation(productId);

        var command = new PlaceOrderCommand(customerId, [new PlaceOrderLineCommand(productId, 2)]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Reserved.ToString(), result.Value.Status);
        Assert.Equal(customerId, result.Value.CustomerId);
    }

    [Fact]
    public async Task HandleAsync_PersistsOrderTwice_WhenAllLinesSucceed()
    {
        var productId = Guid.NewGuid();
        SetupSuccessfulProduct(productId);
        SetupSuccessfulReservation(productId);

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderLineCommand(productId, 1)]);
        await _handler.HandleAsync(command);

        // First save: Pending; second save: Reserved
        await _orderRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SnapshotsProductTitleAndPrice_OntoOrderLine()
    {
        var productId = Guid.NewGuid();
        var product = new GameResponse(productId, "Test Game Title", "desc", 49.99m, 10);
        _catalogClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Success(product));
        SetupSuccessfulReservation(productId);

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderLineCommand(productId, 3)]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var line = result.Value.Lines[0];
        Assert.Equal("Test Game Title", line.ProductTitle);
        Assert.Equal(49.99m, line.UnitPrice);
        Assert.Equal(3, line.Quantity);
    }

    // ── One line fails reserve, all releases succeed → Failed ─────────────────

    [Fact]
    public async Task HandleAsync_ReturnsPlacementFailed_WhenReservationFails()
    {
        var (product1Id, product2Id) = (Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(product1Id);
        SetupSuccessfulProduct(product2Id);
        SetupSuccessfulReservation(product1Id);
        _catalogClient.ReserveStockAsync(product2Id, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(Error.Conflict("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new PlaceOrderLineCommand(product1Id, 1), new PlaceOrderLineCommand(product2Id, 1)]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PlacementFailed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReleasesSuccessfulReservations_WhenOneFails()
    {
        var (product1Id, product2Id) = (Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(product1Id);
        SetupSuccessfulProduct(product2Id);
        SetupSuccessfulReservation(product1Id);
        _catalogClient.ReserveStockAsync(product2Id, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(Error.Conflict("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new PlaceOrderLineCommand(product1Id, 1), new PlaceOrderLineCommand(product2Id, 1)]);

        await _handler.HandleAsync(command);

        await _catalogClient.Received(1)
            .ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>());
        await _catalogClient.DidNotReceive()
            .ReleaseReservationAsync(Arg.Any<Guid>(), product2Id, Arg.Any<CancellationToken>());
    }

    // ── Release fails all retries → CompensationFailed ────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsPlacementFailed_WhenReleaseFailsAllRetries()
    {
        var (product1Id, product2Id, product3Id) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(product1Id);
        SetupSuccessfulProduct(product2Id);
        SetupSuccessfulProduct(product3Id);
        SetupSuccessfulReservation(product1Id);
        SetupSuccessfulReservation(product2Id);
        _catalogClient.ReserveStockAsync(product3Id, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(Error.Conflict("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(Error.Failure("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product2Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [
                new PlaceOrderLineCommand(product1Id, 1),
                new PlaceOrderLineCommand(product2Id, 1),
                new PlaceOrderLineCommand(product3Id, 1)
            ]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.PlacementFailed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RetriesRelease3Times_WhenReleaseFailsAllRetries()
    {
        var (product1Id, product2Id) = (Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(product1Id);
        SetupSuccessfulProduct(product2Id);
        SetupSuccessfulReservation(product1Id);
        _catalogClient.ReserveStockAsync(product2Id, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(Error.Conflict("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(Error.Failure("x", "y")));

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new PlaceOrderLineCommand(product1Id, 1), new PlaceOrderLineCommand(product2Id, 1)]);

        await _handler.HandleAsync(command);

        await _catalogClient.Received(3)
            .ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StillReleasesOtherLines_WhenOneReleaseFails()
    {
        var (product1Id, product2Id, product3Id) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetupSuccessfulProduct(product1Id);
        SetupSuccessfulProduct(product2Id);
        SetupSuccessfulProduct(product3Id);
        SetupSuccessfulReservation(product1Id);
        SetupSuccessfulReservation(product2Id);
        _catalogClient.ReserveStockAsync(product3Id, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(Error.Conflict("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(Error.Failure("x", "y")));
        _catalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), product2Id, Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [
                new PlaceOrderLineCommand(product1Id, 1),
                new PlaceOrderLineCommand(product2Id, 1),
                new PlaceOrderLineCommand(product3Id, 1)
            ]);

        await _handler.HandleAsync(command);

        // product1 release: 3 retries (all failed)
        await _catalogClient.Received(3)
            .ReleaseReservationAsync(Arg.Any<Guid>(), product1Id, Arg.Any<CancellationToken>());
        // product2 release: attempted and succeeded
        await _catalogClient.Received(1)
            .ReleaseReservationAsync(Arg.Any<Guid>(), product2Id, Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupSuccessfulProduct(Guid productId, decimal price = 29.99m)
    {
        _catalogClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Success(
                new GameResponse(productId, "Test Game", null, price, 10)));
    }

    private void SetupSuccessfulReservation(Guid productId)
    {
        _catalogClient.ReserveStockAsync(productId, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Success(
                new ReservationResponse(productId, Guid.NewGuid(), 1, "Reserved", null)));
    }
}
