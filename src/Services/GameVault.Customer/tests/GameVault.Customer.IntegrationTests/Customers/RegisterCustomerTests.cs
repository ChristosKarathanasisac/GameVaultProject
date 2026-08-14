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
public sealed class RegisterCustomerTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;
    private readonly HttpClient _client;

    public RegisterCustomerTests(CustomerApiFactory factory)
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
    public async Task Register_Returns201_WithLocationHeader_WhenSuccessful()
    {
        var keycloakId = Guid.NewGuid();
        _factory.KeycloakMock
            .CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Success(keycloakId));

        var response = await _client.PostAsJsonAsync("/customers/register",
            new RegisterCustomerRequest("alice@example.com", "S3cr3t!1", "Alice", "Smith", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(keycloakId.ToString(), response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Register_Returns400_WhenEmailIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/customers/register",
            new RegisterCustomerRequest("not-an-email", "S3cr3t!1", "Alice", "Smith", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _factory.KeycloakMock
            .DidNotReceiveWithAnyArgs()
            .CreateUserAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Register_Returns400_WhenPasswordIsTooShort()
    {
        var response = await _client.PostAsJsonAsync("/customers/register",
            new RegisterCustomerRequest("alice@example.com", "short", "Alice", "Smith", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Returns409_WhenKeycloakReportsEmailConflict()
    {
        _factory.KeycloakMock
            .CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Failure(CustomerErrors.EmailConflict));

        var response = await _client.PostAsJsonAsync("/customers/register",
            new RegisterCustomerRequest("duplicate@example.com", "S3cr3t!1", "Alice", "Smith", null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_Returns409_WhenEmailBelongsToDeactivatedAccount()
    {
        // Validates that a globally unique email index prevents re-registration after soft-delete.
        // The caller must use the reactivate endpoint instead.
        var existingId = await _factory.SeedCustomerAsync("alice@example.com");
        await _factory.SoftDeleteCustomerAsync(existingId);

        var response = await _client.PostAsJsonAsync("/customers/register",
            new RegisterCustomerRequest("alice@example.com", "S3cr3t!1", "Alice", "Smith", null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await _factory.KeycloakMock
            .DidNotReceiveWithAnyArgs()
            .CreateUserAsync(default!, default!, default!, default!, default);
    }
}
