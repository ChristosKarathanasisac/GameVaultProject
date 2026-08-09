using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.Application.Customers.Delete;

public sealed class DeleteCustomerHandler : IDeleteCustomerHandler
{
    private readonly ICustomerRepository _repository;

    public DeleteCustomerHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Unit>> HandleAsync(Guid routeId, Guid callerId, CancellationToken cancellationToken = default)
    {
        if (routeId != callerId)
            return CustomerErrors.Forbidden;

        var customer = await _repository.GetByIdAsync(routeId, cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound;

        customer.SoftDelete();

        await _repository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
