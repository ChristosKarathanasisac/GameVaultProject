using System.Net;
using GameVault.Customer.IntegrationTests.Fixtures;

namespace GameVault.Customer.IntegrationTests.Customers;

[Collection(nameof(CustomerApiCollection))]
public sealed class DeleteCustomerTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;
    private readonly HttpClient _anonymousClient;

    public DeleteCustomerTests(CustomerApiFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_Returns401_WhenUnauthenticated()
    {
        var response = await _anonymousClient.DeleteAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_WhenOwnerDeletesOwnProfile()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var client = _factory.CreateAuthenticatedClient(customerId);

        var response = await client.DeleteAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_WhenCallerIsNotOwner()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.DeleteAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenCustomerDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(nonExistentId);

        var response = await client.DeleteAsync($"/customers/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_MakesCustomerInvisibleToSubsequentGet()
    {
        // Validates the full soft-delete path end-to-end against a real database:
        // the IsDeleted filter in CustomerRepository.GetByIdAsync must exclude the row.
        var customerId = await _factory.SeedCustomerAsync();
        var client = _factory.CreateAuthenticatedClient(customerId);

        await client.DeleteAsync($"/customers/{customerId}");
        var getResponse = await client.GetAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
