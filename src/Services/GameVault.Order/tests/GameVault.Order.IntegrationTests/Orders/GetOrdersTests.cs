using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.IntegrationTests.Fixtures;

namespace GameVault.Order.IntegrationTests.Orders;

[Collection(nameof(OrderApiCollection))]
public sealed class GetOrdersTests : IAsyncLifetime
{
    private readonly OrderApiFactory _factory;
    private readonly Guid _customerId = Guid.NewGuid();
    private HttpClient _client = null!;

    public GetOrdersTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _client = _factory.CreateAuthenticatedClient(_customerId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/orders ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListMyOrders_Returns401_WhenNotAuthenticated()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListMyOrders_ReturnsEmptyList_WhenCallerHasNoOrders()
    {
        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.NotNull(paged);
        Assert.Empty(paged.Items);
        Assert.Equal(0, paged.TotalCount);
    }

    [Fact]
    public async Task ListMyOrders_ReturnsOnlyCallerOrders_WhenMultipleCustomersHaveOrders()
    {
        var otherCustomerId = Guid.NewGuid();
        await _factory.SeedOrdersAsync(_customerId, 2);
        await _factory.SeedOrderAsync(otherCustomerId);

        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.NotNull(paged);
        Assert.Equal(2, paged.TotalCount);
        Assert.All(paged.Items, o => Assert.Equal(_customerId, o.CustomerId));
    }

    [Fact]
    public async Task ListMyOrders_RespectsPageSize_WhenCallerHasMoreOrdersThanPageSize()
    {
        await _factory.SeedOrdersAsync(_customerId, 5);

        var response = await _client.GetAsync("/api/orders?page=1&pageSize=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.NotNull(paged);
        Assert.Equal(3, paged.Items.Count);
        Assert.Equal(5, paged.TotalCount);
        Assert.Equal(2, paged.TotalPages);
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderById_Returns401_WhenNotAuthenticated()
    {
        var orderId = await _factory.SeedOrderAsync(_customerId);
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_Returns404_WhenOrderDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_Returns404_WhenOrderBelongsToAnotherCustomer()
    {
        var otherCustomerId = Guid.NewGuid();
        var orderId = await _factory.SeedOrderAsync(otherCustomerId);

        var response = await _client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_Returns200_WithOrderResponse_WhenCallerOwnsTheOrder()
    {
        var orderId = await _factory.SeedOrderAsync(_customerId);

        var response = await _client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(orderId, order.Id);
        Assert.Equal(_customerId, order.CustomerId);
        Assert.NotEmpty(order.Lines);
    }
}
