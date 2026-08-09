using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.Application.Abstractions;

public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerEntity customer, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
