using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Customers.Delete;
using GameVault.Customer.Application.Errors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Application;

public sealed class DeleteCustomerHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IKeycloakAdminClient _keycloak = Substitute.For<IKeycloakAdminClient>();
    private readonly DeleteCustomerHandler _handler;

    public DeleteCustomerHandlerTests()
    {
        _handler = new DeleteCustomerHandler(_repository, _keycloak);
    }

    [Fact]
    public async Task HandleAsync_ReturnsForbidden_WhenCallerIsNotResourceOwner()
    {
        var result = await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.Forbidden, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverQueriesRepository_WhenCallerIsNotResourceOwner()
    {
        await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        var result = await _handler.HandleAsync(id, id);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesCustomerAndPersists_WhenRequestIsValid()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.HandleAsync(id, id);

        Assert.True(result.IsSuccess);
        Assert.True(customer.IsDeleted);
        Assert.NotNull(customer.DeletedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DeletesKeycloakUser_WhenRequestIsValid()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);

        await _handler.HandleAsync(id, id);

        await _keycloak.Received(1).DeleteUserAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NeverPersistsLocally_WhenKeycloakDeletionFails()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);
        _keycloak.DeleteUserAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Keycloak down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(id, id));

        Assert.False(customer.IsDeleted);
        Assert.Null(customer.DeletedAt);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_NeverPersists_WhenForbidden()
    {
        await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_NeverTouchesKeycloak_WhenForbidden()
    {
        await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await _keycloak.DidNotReceiveWithAnyArgs().DeleteUserAsync(default, default);
    }

    [Fact]
    public async Task HandleAsync_NeverPersists_WhenCustomerNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        await _handler.HandleAsync(id, id);

        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_NeverTouchesKeycloak_WhenCustomerNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        await _handler.HandleAsync(id, id);

        await _keycloak.DidNotReceiveWithAnyArgs().DeleteUserAsync(default, default);
    }
}
