using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.Reactivate;

public sealed class ReactivateCustomerHandler : IReactivateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IKeycloakAdminClient _keycloakClient;

    public ReactivateCustomerHandler(ICustomerRepository repository, IKeycloakAdminClient keycloakClient)
    {
        _repository = repository;
        _keycloakClient = keycloakClient;
    }

    public async Task<Result<Unit>> HandleAsync(ReactivateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validationError =
            CustomerRequestValidation.ValidateEmail(request.Email) ??
            CustomerRequestValidation.ValidatePassword(request.Password);

        if (validationError is not null)
            return validationError;

        var customer = await _repository.GetDeletedByEmailAsync(request.Email, cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound;

        var keycloakResult = await _keycloakClient.ReactivateUserAsync(
            customer.Id,
            customer.Email,
            request.Password,
            customer.FirstName,
            customer.LastName,
            cancellationToken);

        if (keycloakResult.IsFailure)
            return keycloakResult.Error;

        try
        {
            customer.Reactivate();
            await _repository.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
        catch
        {
            await _keycloakClient.DeleteUserAsync(customer.Id, cancellationToken);
            throw;
        }
    }
}
