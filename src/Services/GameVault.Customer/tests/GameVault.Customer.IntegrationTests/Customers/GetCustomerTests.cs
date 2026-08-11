using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Responses.Customer;
using GameVault.Customer.IntegrationTests.Fixtures;

namespace GameVault.Customer.IntegrationTests.Customers;

[Collection(nameof(CustomerApiCollection))]
public sealed class GetCustomerTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;
    private readonly HttpClient _anonymousClient;

    public GetCustomerTests(CustomerApiFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetById_Returns401_WhenUnauthenticated()
    {
        var response = await _anonymousClient.GetAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenCustomerDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(nonExistentId);

        var response = await client.GetAsync($"/customers/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns200_WithCorrectData_WhenCustomerExists()
    {
        var customerId = await _factory.SeedCustomerAsync(
            "alice@example.com", "Alice", "Smith", "+1234567890");
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.GetAsync($"/customers/{customerId}");
        var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(customerId, body.Id);
        Assert.Equal("alice@example.com", body.Email);
        Assert.Equal("Alice", body.FirstName);
        Assert.Equal("Smith", body.LastName);
        Assert.Equal("+1234567890", body.PhoneNumber);
    }

    [Fact]
    public async Task GetById_Returns403_WhenCallerIsNotOwner()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenCustomerIsSoftDeleted()
    {
        // Validates that the repository's IsDeleted filter is enforced at the database level.
        var customerId = await _factory.SeedCustomerAsync();
        await _factory.SoftDeleteCustomerAsync(customerId);
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.GetAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Returns200_WithAuthenticatedUsersData()
    {
        var customerId = await _factory.SeedCustomerAsync("me@example.com", "Own", "Profile");
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.GetAsync("/customers/me");
        var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(customerId, body.Id);
        Assert.Equal("me@example.com", body.Email);
    }
}
