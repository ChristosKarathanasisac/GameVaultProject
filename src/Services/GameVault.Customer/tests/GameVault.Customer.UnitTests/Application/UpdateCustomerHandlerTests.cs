using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Customers.Update;
using GameVault.Customer.Application.Errors;
using NSubstitute;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Application;

public sealed class UpdateCustomerHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly UpdateCustomerHandler _handler;

    public UpdateCustomerHandlerTests()
    {
        _handler = new UpdateCustomerHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsForbidden_WhenCallerIsNotResourceOwner()
    {
        var result = await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), AnyRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.Forbidden, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NeverQueriesRepository_WhenCallerIsNotResourceOwner()
    {
        await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), AnyRequest());

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        var result = await _handler.HandleAsync(id, id, AnyRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_UpdatesCustomerAndPersists_WhenRequestIsValid()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);
        var request = new UpdateCustomerRequest("Alicia", "Jones", "+9999999999");

        var result = await _handler.HandleAsync(id, id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alicia", customer.FirstName);
        Assert.Equal("Jones", customer.LastName);
        Assert.Equal("+9999999999", customer.PhoneNumber);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NeverPersists_WhenForbidden()
    {
        await _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), AnyRequest());

        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static UpdateCustomerRequest AnyRequest() =>
        new("First", "Last", null);
}
