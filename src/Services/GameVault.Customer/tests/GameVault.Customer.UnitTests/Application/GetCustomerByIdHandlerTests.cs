using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Customers.GetById;
using GameVault.Customer.Application.Errors;
using GameVault.Customer.Domain.Enums;
using NSubstitute;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Application;

public sealed class GetCustomerByIdHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly GetCustomerByIdHandler _handler;

    public GetCustomerByIdHandlerTests()
    {
        _handler = new GetCustomerByIdHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CustomerEntity?)null);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCustomerResponse_WhenCustomerExists()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", "+1234567890");
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.HandleAsync(id);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("alice@example.com", result.Value.Email);
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        Assert.Equal("+1234567890", result.Value.PhoneNumber);
        Assert.Equal(RegistrationStatus.Completed.ToString(), result.Value.RegistrationStatus);
    }

    [Fact]
    public async Task HandleAsync_MapsPhoneNumber_AsNullWhenAbsent()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "bob@example.com", "Bob", "Jones", null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.HandleAsync(id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PhoneNumber);
    }
}
