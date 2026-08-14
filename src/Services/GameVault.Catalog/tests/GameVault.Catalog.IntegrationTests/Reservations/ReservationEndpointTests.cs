using System.Net;
using System.Net.Http.Json;
using GameVault.Catalog.IntegrationTests.Fixtures;
using GameVault.Contracts.Requests.Catalog;
using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.IntegrationTests.Reservations;

[Collection(nameof(CatalogApiCollection))]
public sealed class ReservationEndpointTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public ReservationEndpointTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // --- Reserve ---

    [Fact]
    public async Task Reserve_Returns200_WithReservationResponse_WhenStockIsAvailable()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var request = new ReserveStockRequest(Guid.NewGuid(), 3);

        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/reservations", request);
        var body = await response.Content.ReadFromJsonAsync<ReservationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(productId, body.ProductId);
        Assert.Equal(request.OrderId, body.OrderId);
        Assert.Equal(3, body.Quantity);
        Assert.Equal("Reserved", body.Status);
        Assert.NotNull(body.ExpiresAt);
    }

    [Fact]
    public async Task Reserve_Returns200_WithExistingReservation_WhenSameOrderAndProduct()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();
        var request = new ReserveStockRequest(orderId, 3);

        var first = await _client.PostAsJsonAsync($"/api/products/{productId}/reservations", request);
        var second = await _client.PostAsJsonAsync($"/api/products/{productId}/reservations", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        Assert.Equal(7, await _factory.GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task Reserve_Returns404_WhenProductDoesNotExist()
    {
        var request = new ReserveStockRequest(Guid.NewGuid(), 1);

        var response = await _client.PostAsJsonAsync($"/api/products/{Guid.NewGuid()}/reservations", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_Returns400_WhenQuantityIsZero()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var request = new ReserveStockRequest(Guid.NewGuid(), 0);

        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/reservations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_Returns409_WhenInsufficientStock()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 2);
        var request = new ReserveStockRequest(Guid.NewGuid(), 5);

        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/reservations", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- Confirm ---

    [Fact]
    public async Task Confirm_Returns200_WhenReservationIsInReservedState()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();
        await _factory.SeedReservationAsync(productId, orderId, 3);

        var response = await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/confirm", null);
        var body = await response.Content.ReadFromJsonAsync<ReservationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Confirmed", body.Status);
    }

    [Fact]
    public async Task Confirm_Returns404_WhenReservationDoesNotExist()
    {
        var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/products/{Guid.NewGuid()}/reservations/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Returns409_WhenReservationAlreadyConfirmed()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();
        await _factory.SeedReservationAsync(productId, orderId, 3);

        await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/confirm", null);
        var second = await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // --- Release ---

    [Fact]
    public async Task Release_Returns200_AndRestoresStock_WhenReservationIsInReservedState()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();
        await _factory.SeedReservationAsync(productId, orderId, 4);

        var stockAfterReserve = await _factory.GetAvailableStockAsync(productId);
        Assert.Equal(6, stockAfterReserve);

        var response = await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/release", null);
        var body = await response.Content.ReadFromJsonAsync<ReservationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Released", body.Status);
        Assert.Equal(10, await _factory.GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task Release_Returns404_WhenReservationDoesNotExist()
    {
        var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/products/{Guid.NewGuid()}/reservations/release", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Release_Returns409_WhenReservationAlreadyReleased()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();
        await _factory.SeedReservationAsync(productId, orderId, 3);

        await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/release", null);
        var second = await _client.PostAsync($"/api/orders/{orderId}/products/{productId}/reservations/release", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
