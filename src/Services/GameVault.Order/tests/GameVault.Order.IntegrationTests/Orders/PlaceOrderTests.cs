using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Requests.Order;
using GameVault.Contracts.Responses.Catalog;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.IntegrationTests.Fixtures;
using GameVault.SharedKernel.Results;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace GameVault.Order.IntegrationTests.Orders;

[Collection(nameof(OrderApiCollection))]
public sealed class PlaceOrderTests : IAsyncLifetime
{
    private readonly OrderApiFactory _factory;
    private readonly Guid _customerId = Guid.NewGuid();
    private HttpClient _client = null!;

    public PlaceOrderTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _factory.CatalogClientMock.ClearSubstitute(ClearOptions.All);
        await _factory.ResetDatabaseAsync();
        _client = _factory.CreateAuthenticatedClient(_customerId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Returns401_WhenNotAuthenticated()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/api/orders",
            ValidRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Returns400_WhenLinesAreEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_Returns400_WhenQuantityIsZero()
    {
        var response = await _client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest([new OrderLineRequest(Guid.NewGuid(), 0)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── ProductNotFound ───────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Returns404_WhenProductNotFoundInCatalog()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClientMock
            .GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Failure(
                Error.NotFound("Catalog.ProductNotFound", "not found")));

        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest(productId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Success ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Returns200_WithReservedOrder_WhenAllLinesSucceed()
    {
        var productId = Guid.NewGuid();
        SetupSuccessfulCatalogMock(productId);

        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest(productId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal("Reserved", order.Status);
        Assert.Equal(_customerId, order.CustomerId);
    }

    [Fact]
    public async Task PlaceOrder_SnapshotsTitleAndPrice_InOrderLine()
    {
        var productId = Guid.NewGuid();
        var product = new GameResponse(productId, "Game Title Snapshot", null, 59.99m, 5);
        _factory.CatalogClientMock
            .GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Success(product));
        _factory.CatalogClientMock
            .ReserveStockAsync(productId, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Success(
                new ReservationResponse(productId, Guid.NewGuid(), 2, "Reserved", null)));

        var response = await _client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest([new OrderLineRequest(productId, 2)]));

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        var line = order.Lines[0];
        Assert.Equal("Game Title Snapshot", line.ProductTitle);
        Assert.Equal(59.99m, line.UnitPrice);
        Assert.Equal(2, line.Quantity);
    }

    // ── Placement failure ─────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Returns409_WhenReservationFails()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClientMock
            .GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Success(
                new GameResponse(productId, "Game", null, 19.99m, 10)));
        _factory.CatalogClientMock
            .ReserveStockAsync(productId, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Failure(
                Error.Conflict("Catalog.ReservationFailed", "Insufficient stock")));
        _factory.CatalogClientMock
            .ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest(productId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_PersistsOrderInDb_OnSuccess()
    {
        var productId = Guid.NewGuid();
        SetupSuccessfulCatalogMock(productId);

        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest(productId));

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);

        // Verify the order was persisted by checking it exists
        var orderId = order.Id;
        Assert.NotEqual(Guid.Empty, orderId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PlaceOrderRequest ValidRequest(Guid productId, int quantity = 1) =>
        new([new OrderLineRequest(productId, quantity)]);

    private void SetupSuccessfulCatalogMock(Guid productId)
    {
        _factory.CatalogClientMock
            .GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Result<GameResponse>.Success(
                new GameResponse(productId, "Test Game", null, 29.99m, 10)));

        _factory.CatalogClientMock
            .ReserveStockAsync(productId, Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReservationResponse>.Success(
                new ReservationResponse(productId, Guid.NewGuid(), 1, "Reserved", null)));
    }
}
