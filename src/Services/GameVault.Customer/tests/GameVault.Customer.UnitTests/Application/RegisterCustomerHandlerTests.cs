using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Customers.Register;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Application;

public sealed class RegisterCustomerHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IKeycloakAdminClient _keycloak = Substitute.For<IKeycloakAdminClient>();
    private readonly RegisterCustomerHandler _handler;

    public RegisterCustomerHandlerTests()
    {
        _handler = new RegisterCustomerHandler(_repository, _keycloak);
    }

    [Fact]
    public async Task HandleAsync_ReturnsKeycloakUserId_WhenRegistrationSucceeds()
    {
        var keycloakUserId = Guid.NewGuid();
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Success(keycloakUserId));

        var result = await _handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(keycloakUserId, result.Value);
    }

    [Fact]
    public async Task HandleAsync_PersistsCustomer_WhenKeycloakSucceeds()
    {
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Success(Guid.NewGuid()));

        await _handler.HandleAsync(request);

        await _repository.Received(1).AddAsync(Arg.Any<CustomerEntity>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "S3cr3t!1", "Alice", "Smith")]
    [InlineData("not-an-email", "S3cr3t!1", "Alice", "Smith")]
    [InlineData("alice@example.com", "short", "Alice", "Smith")]
    [InlineData("alice@example.com", "S3cr3t!1", "", "Smith")]
    [InlineData("alice@example.com", "S3cr3t!1", "Alice", "")]
    public async Task HandleAsync_ReturnsValidationFailure_ForInvalidRequests(
        string email, string password, string firstName, string lastName)
    {
        var request = new RegisterCustomerRequest(email, password, firstName, lastName, null);

        var result = await _handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenNameExceedsMaxLength()
    {
        var request = new RegisterCustomerRequest(
            "alice@example.com", "S3cr3t!1", new string('A', 101), "Smith", null);

        var result = await _handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenPhoneNumberExceedsMaxLength()
    {
        var request = new RegisterCustomerRequest(
            "alice@example.com", "S3cr3t!1", "Alice", "Smith", new string('1', 31));

        var result = await _handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_NeverCallsKeycloakOrRepository_WhenValidationFails()
    {
        var request = new RegisterCustomerRequest("", "S3cr3t!1", "Alice", "Smith", null);

        await _handler.HandleAsync(request);

        await _keycloak.DidNotReceiveWithAnyArgs()
            .CreateUserAsync(default!, default!, default!, default!, default);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmailConflict_WhenKeycloakRejectsEmail()
    {
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Failure(CustomerErrors.EmailConflict));

        var result = await _handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.EmailConflict, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverTouchesRepository_WhenKeycloakFails()
    {
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Failure(CustomerErrors.EmailConflict));

        await _handler.HandleAsync(request);

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_CompensatesKeycloak_WhenDatabaseThrows()
    {
        var keycloakUserId = Guid.NewGuid();
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Success(keycloakUserId));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(request));

        await _keycloak.Received(1).DeleteUserAsync(keycloakUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RethrowsOriginalException_AfterKeycloakCompensation()
    {
        var request = ValidRequest();
        _keycloak.CreateUserAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Guid>.Success(Guid.NewGuid()));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(request));

        Assert.Equal("DB down", ex.Message);
    }

    private static RegisterCustomerRequest ValidRequest() =>
        new("alice@example.com", "S3cr3t!1", "Alice", "Smith", null);
}
