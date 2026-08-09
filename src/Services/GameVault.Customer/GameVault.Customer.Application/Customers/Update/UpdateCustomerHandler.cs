using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.Application.Customers.Update;

public sealed class UpdateCustomerHandler : IUpdateCustomerHandler
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomerHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Unit>> HandleAsync(Guid routeId, Guid callerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (routeId != callerId)
            return CustomerErrors.Forbidden;

        var customer = await _repository.GetByIdAsync(routeId, cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound;

        customer.Update(request.FirstName, request.LastName, request.PhoneNumber);

        await _repository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
