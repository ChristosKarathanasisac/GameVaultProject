using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.Application.Customers.Delete;

public sealed class DeleteCustomerHandler : IDeleteCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IKeycloakAdminClient _keycloakClient;

    public DeleteCustomerHandler(ICustomerRepository repository, IKeycloakAdminClient keycloakClient)
    {
        _repository = repository;
        _keycloakClient = keycloakClient;
    }

    public async Task<Result<Unit>> HandleAsync(Guid routeId, Guid callerId, CancellationToken cancellationToken = default)
    {
        if (routeId != callerId)
            return CustomerErrors.Forbidden;

        var customer = await _repository.GetByIdAsync(routeId, cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound;

        var keycloakResult = await _keycloakClient.DeleteUserAsync(routeId, cancellationToken);

        if (keycloakResult.IsFailure)
            return keycloakResult.Error;

        customer.SoftDelete();

        await _repository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
