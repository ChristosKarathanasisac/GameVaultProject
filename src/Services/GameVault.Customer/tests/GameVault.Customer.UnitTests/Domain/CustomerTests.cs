using GameVault.Customer.Domain.Enums;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.UnitTests.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", "+1234567890");

        Assert.Equal(id, customer.Id);
        Assert.Equal("alice@example.com", customer.Email);
        Assert.Equal("Alice", customer.FirstName);
        Assert.Equal("Smith", customer.LastName);
        Assert.Equal("+1234567890", customer.PhoneNumber);
        Assert.Equal(RegistrationStatus.Completed, customer.RegistrationStatus);
        Assert.False(customer.IsDeleted);
        Assert.Null(customer.DeletedAt);
        Assert.True(customer.CreatedAt >= before);
        Assert.True(customer.UpdatedAt >= before);
    }

    [Fact]
    public void Create_WithNullPhoneNumber_IsAllowed()
    {
        var customer = CustomerEntity.Create(Guid.NewGuid(), "bob@example.com", "Bob", "Jones", null);

        Assert.Null(customer.PhoneNumber);
    }

    [Fact]
    public void Update_MutatesOnlyMutableFields()
    {
        var customer = CustomerEntity.Create(Guid.NewGuid(), "alice@example.com", "Alice", "Smith", null);
        var originalEmail = customer.Email;
        var originalId = customer.Id;
        var originalCreatedAt = customer.CreatedAt;
        var beforeUpdate = DateTime.UtcNow;

        customer.Update("Alicia", "Jones", "+9999999999");

        Assert.Equal("Alicia", customer.FirstName);
        Assert.Equal("Jones", customer.LastName);
        Assert.Equal("+9999999999", customer.PhoneNumber);
        Assert.Equal(originalEmail, customer.Email);
        Assert.Equal(originalId, customer.Id);
        Assert.Equal(originalCreatedAt, customer.CreatedAt);
        Assert.True(customer.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public void Update_ClearsPhoneNumber_WhenPassedNull()
    {
        var customer = CustomerEntity.Create(Guid.NewGuid(), "alice@example.com", "Alice", "Smith", "+1234567890");

        customer.Update("Alice", "Smith", null);

        Assert.Null(customer.PhoneNumber);
    }

    [Fact]
    public void SoftDelete_MarksCustomerAsDeleted()
    {
        var customer = CustomerEntity.Create(Guid.NewGuid(), "alice@example.com", "Alice", "Smith", null);
        var beforeDelete = DateTime.UtcNow;

        customer.SoftDelete();

        Assert.True(customer.IsDeleted);
        Assert.NotNull(customer.DeletedAt);
        Assert.True(customer.DeletedAt >= beforeDelete);
        Assert.True(customer.UpdatedAt >= beforeDelete);
    }

    [Fact]
    public void SoftDelete_DoesNotAffectEmail_OrId()
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, "alice@example.com", "Alice", "Smith", null);

        customer.SoftDelete();

        Assert.Equal(id, customer.Id);
        Assert.Equal("alice@example.com", customer.Email);
    }
}
