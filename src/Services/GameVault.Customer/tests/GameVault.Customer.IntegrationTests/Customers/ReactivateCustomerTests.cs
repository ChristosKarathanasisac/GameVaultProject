using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Errors;
using GameVault.Customer.IntegrationTests.Fixtures;
using GameVault.SharedKernel.Results;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace GameVault.Customer.IntegrationTests.Customers;

[Collection(nameof(CustomerApiCollection))]
public sealed class ReactivateCustomerTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;
    private readonly HttpClient _client;

    public ReactivateCustomerTests(CustomerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        _factory.KeycloakMock.ClearSubstitute(ClearOptions.All);
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Reactivate_Returns204_WhenSuccessful()
    {
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        await _factory.SoftDeleteCustomerAsync(customerId);

        _factory.KeycloakMock
            .ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Success(Unit.Value));

        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("alice@example.com", "S3cr3t!1"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_ReactivatesWithExistingCustomerId()
    {
        // Validates that the same Customer.Id (= Keycloak UUID) is preserved after reactivation.
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        await _factory.SoftDeleteCustomerAsync(customerId);

        _factory.KeycloakMock
            .ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Success(Unit.Value));

        await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("alice@example.com", "S3cr3t!1"));

        await _factory.KeycloakMock
            .Received(1)
            .ReactivateUserAsync(customerId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivate_Returns404_WhenNoSoftDeletedCustomerWithThatEmailExists()
    {
        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("nobody@example.com", "S3cr3t!1"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_Returns404_WhenCustomerIsActiveNotDeleted()
    {
        // An active account is not a soft-deleted account — reactivation should not find it.
        await _factory.SeedCustomerAsync("active@example.com");

        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("active@example.com", "S3cr3t!1"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_Returns400_WhenEmailIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("not-an-email", "S3cr3t!1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _factory.KeycloakMock
            .DidNotReceiveWithAnyArgs()
            .ReactivateUserAsync(default, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Reactivate_Returns400_WhenPasswordIsTooShort()
    {
        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("alice@example.com", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_Returns400_WhenKeycloakRejectsPassword()
    {
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        await _factory.SoftDeleteCustomerAsync(customerId);

        _factory.KeycloakMock
            .ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Failure(
                CustomerErrors.RegistrationRejectedByKeycloak("Password policy not met")));

        var response = await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("alice@example.com", "S3cr3t!1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_AllowsGetById_AfterReactivation()
    {
        // Validates that the reactivated account is accessible again at its original URL.
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");
        await _factory.SoftDeleteCustomerAsync(customerId);

        _factory.KeycloakMock
            .ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Success(Unit.Value));

        await _client.PostAsJsonAsync("/customers/reactivate",
            new ReactivateCustomerRequest("alice@example.com", "S3cr3t!1"));

        var authClient = _factory.CreateAuthenticatedClient(customerId);
        var getResponse = await authClient.GetAsync($"/customers/{customerId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
