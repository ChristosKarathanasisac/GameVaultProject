using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Customers.Reactivate;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Application;

public sealed class ReactivateCustomerHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IKeycloakAdminClient _keycloak = Substitute.For<IKeycloakAdminClient>();
    private readonly ReactivateCustomerHandler _handler;

    public ReactivateCustomerHandlerTests()
    {
        _handler = new ReactivateCustomerHandler(_repository, _keycloak);
    }

    [Theory]
    [InlineData("", "S3cr3t!1")]
    [InlineData("not-an-email", "S3cr3t!1")]
    [InlineData("alice@example.com", "short")]
    public async Task HandleAsync_ReturnsValidationFailure_ForInvalidRequests(string email, string password)
    {
        var result = await _handler.HandleAsync(new ReactivateCustomerRequest(email, password));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_NeverQueriesRepository_WhenValidationFails()
    {
        await _handler.HandleAsync(new ReactivateCustomerRequest("bad-email", "S3cr3t!1"));

        await _repository.DidNotReceiveWithAnyArgs().GetDeletedByEmailAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_NeverTouchesKeycloak_WhenValidationFails()
    {
        await _handler.HandleAsync(new ReactivateCustomerRequest("bad-email", "S3cr3t!1"));

        await _keycloak.DidNotReceiveWithAnyArgs()
            .ReactivateUserAsync(default, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenNoSoftDeletedCustomerExists()
    {
        _repository.GetDeletedByEmailAsync("alice@example.com", Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        var result = await _handler.HandleAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverTouchesKeycloak_WhenCustomerNotFound()
    {
        _repository.GetDeletedByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        await _handler.HandleAsync(ValidRequest());

        await _keycloak.DidNotReceiveWithAnyArgs()
            .ReactivateUserAsync(default, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_ReactivatesCustomer_WhenSuccessful()
    {
        var customer = SoftDeletedCustomer();
        SetupSuccessfulReactivation(customer);

        var result = await _handler.HandleAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.False(customer.IsDeleted);
        Assert.Null(customer.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_PersistsReactivation_WhenSuccessful()
    {
        var customer = SoftDeletedCustomer();
        SetupSuccessfulReactivation(customer);

        await _handler.HandleAsync(ValidRequest());

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CallsKeycloakWithExistingCustomerId_WhenCustomerFound()
    {
        var customer = SoftDeletedCustomer();
        SetupSuccessfulReactivation(customer);

        await _handler.HandleAsync(ValidRequest());

        await _keycloak.Received(1).ReactivateUserAsync(
            customer.Id,
            customer.Email,
            ValidRequest().Password,
            customer.FirstName,
            customer.LastName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsReactivationConflict_WhenKeycloakRejectsWithConflict()
    {
        var customer = SoftDeletedCustomer();
        _repository.GetDeletedByEmailAsync(customer.Email, Arg.Any<CancellationToken>())
            .Returns(customer);
        _keycloak.ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Failure(CustomerErrors.ReactivationConflict));

        var result = await _handler.HandleAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.ReactivationConflict, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenKeycloakRejectsPassword()
    {
        var customer = SoftDeletedCustomer();
        _repository.GetDeletedByEmailAsync(customer.Email, Arg.Any<CancellationToken>())
            .Returns(customer);
        _keycloak.ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Failure(
                CustomerErrors.RegistrationRejectedByKeycloak("Password policy not met")));

        var result = await _handler.HandleAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_NeverPersists_WhenKeycloakFails()
    {
        var customer = SoftDeletedCustomer();
        _repository.GetDeletedByEmailAsync(customer.Email, Arg.Any<CancellationToken>())
            .Returns(customer);
        _keycloak.ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Failure(CustomerErrors.ReactivationConflict));

        await _handler.HandleAsync(ValidRequest());

        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_CompensatesKeycloak_WhenDatabaseThrows()
    {
        var customer = SoftDeletedCustomer();
        SetupSuccessfulReactivation(customer);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(ValidRequest()));

        await _keycloak.Received(1).DeleteUserAsync(customer.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RethrowsOriginalException_AfterKeycloakCompensation()
    {
        var customer = SoftDeletedCustomer();
        SetupSuccessfulReactivation(customer);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(ValidRequest()));

        Assert.Equal("DB down", ex.Message);
    }

    private static ReactivateCustomerRequest ValidRequest() =>
        new("alice@example.com", "S3cr3t!1");

    private static CustomerEntity SoftDeletedCustomer()
    {
        var customer = CustomerEntity.Create(Guid.NewGuid(), "alice@example.com", "Alice", "Smith", null);
        customer.SoftDelete();
        return customer;
    }

    private void SetupSuccessfulReactivation(CustomerEntity customer)
    {
        _repository.GetDeletedByEmailAsync(customer.Email, Arg.Any<CancellationToken>())
            .Returns(customer);
        _keycloak.ReactivateUserAsync(default, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<Unit>.Success(Unit.Value));
    }
}
