using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.IntegrationTests.Fixtures;

namespace GameVault.Customer.IntegrationTests.Customers;

[Collection(nameof(CustomerApiCollection))]
public sealed class UpdateCustomerTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;
    private readonly HttpClient _anonymousClient;

    public UpdateCustomerTests(CustomerApiFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Update_Returns401_WhenUnauthenticated()
    {
        var response = await _anonymousClient.PutAsJsonAsync(
            $"/customers/{Guid.NewGuid()}",
            new UpdateCustomerRequest("First", "Last", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns204_WhenOwnerUpdatesOwnProfile()
    {
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.PutAsJsonAsync(
            $"/customers/{customerId}",
            new UpdateCustomerRequest("Alicia", "Jones", "+9999999999"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns400_WhenFirstNameIsEmpty()
    {
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.PutAsJsonAsync(
            $"/customers/{customerId}",
            new UpdateCustomerRequest("", "Jones", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns403_WhenCallerIsNotOwner()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.PutAsJsonAsync(
            $"/customers/{customerId}",
            new UpdateCustomerRequest("Hacker", "McHack", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns404_WhenCustomerDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(nonExistentId);

        var response = await client.PutAsJsonAsync(
            $"/customers/{nonExistentId}",
            new UpdateCustomerRequest("First", "Last", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
