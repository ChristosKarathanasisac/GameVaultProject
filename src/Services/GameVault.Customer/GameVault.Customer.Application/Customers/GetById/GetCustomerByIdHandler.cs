using GameVault.Contracts.Responses.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.GetById;

public sealed class GetCustomerByIdHandler : IGetCustomerByIdHandler
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CustomerResponse>> HandleAsync(Guid routeId, Guid callerId, CancellationToken cancellationToken = default)
    {
        if (routeId != callerId)
            return CustomerErrors.Forbidden;

        var customer = await _repository.GetByIdAsync(routeId, cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound;

        return new CustomerResponse(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            customer.RegistrationStatus.ToString());
    }
}
