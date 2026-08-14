using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Application.Reservations.ReserveStock;
using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Domain.Enums;
using NSubstitute;

namespace GameVault.Catalog.UnitTests.Application;

public sealed class ReserveStockHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IStockReservationRepository _reservationRepository = Substitute.For<IStockReservationRepository>();
    private readonly ReserveStockHandler _handler;

    public ReserveStockHandlerTests()
    {
        _handler = new ReserveStockHandler(_productRepository, _reservationRepository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsReservationResponse_OnHappyPath()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = CreateProductWithStock(productId, 10);

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.HandleAsync(new ReserveStockCommand(productId, orderId, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(productId, result.Value.ProductId);
        Assert.Equal(orderId, result.Value.OrderId);
        Assert.Equal(3, result.Value.Quantity);
        Assert.Equal(ReservationStatus.Reserved.ToString(), result.Value.Status);
        Assert.NotNull(result.Value.ExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_DecreasesAvailableStock_OnHappyPath()
    {
        var productId = Guid.NewGuid();
        var product = CreateProductWithStock(productId, 10);

        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), productId, Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.HandleAsync(new ReserveStockCommand(productId, Guid.NewGuid(), 4));

        Assert.Equal(6, product.Stock!.AvailableStock);
    }

    [Fact]
    public async Task HandleAsync_PersistsBothChanges_OnHappyPath()
    {
        var productId = Guid.NewGuid();
        var product = CreateProductWithStock(productId, 5);

        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), productId, Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.HandleAsync(new ReserveStockCommand(productId, Guid.NewGuid(), 2));

        await _reservationRepository.Received(1).AddAsync(Arg.Any<StockReservation>(), Arg.Any<CancellationToken>());
        await _productRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsInsufficientStock_WhenStockIsInsufficient()
    {
        var productId = Guid.NewGuid();
        var product = CreateProductWithStock(productId, 2);

        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), productId, Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.HandleAsync(new ReserveStockCommand(productId, Guid.NewGuid(), 5));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.InsufficientStock, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotPersist_WhenStockIsInsufficient()
    {
        var productId = Guid.NewGuid();
        var product = CreateProductWithStock(productId, 1);

        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), productId, Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.HandleAsync(new ReserveStockCommand(productId, Guid.NewGuid(), 10));

        await _reservationRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _productRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsExistingReservation_WhenDuplicateCallForSameOrderAndProduct()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var existing = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.HandleAsync(new ReserveStockCommand(productId, orderId, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(orderId, result.Value.OrderId);
        Assert.Equal(productId, result.Value.ProductId);
    }

    [Fact]
    public async Task HandleAsync_DoesNotQueryProductOrPersist_WhenDuplicateReservationExists()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var existing = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.HandleAsync(new ReserveStockCommand(productId, orderId, 3));

        await _productRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await _reservationRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _productRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task HandleAsync_ReturnsInvalidQuantity_WhenQuantityIsZeroOrNegative(int quantity)
    {
        var result = await _handler.HandleAsync(new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), quantity));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverQueriesRepository_WhenQuantityIsInvalid()
    {
        await _handler.HandleAsync(new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        await _reservationRepository.DidNotReceiveWithAnyArgs().GetByOrderAndProductAsync(default, default, default);
        await _productRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsProductNotFound_WhenProductDoesNotExist()
    {
        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var result = await _handler.HandleAsync(new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 1));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.ProductNotFound, result.Error);
    }

    private static Product CreateProductWithStock(Guid productId, int availableStock)
    {
        var product = Product.Create("Test Game", null, 9.99m);
        var stock = Stock.Create(productId, availableStock);

        // Wire the navigation via reflection so the handler can read product.Stock
        typeof(Product)
            .GetProperty(nameof(Product.Stock))!
            .SetValue(product, stock);

        return product;
    }
}
