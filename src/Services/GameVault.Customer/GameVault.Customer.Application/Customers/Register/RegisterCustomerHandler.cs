using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Abstractions;
using GameVault.SharedKernel.Results;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.Application.Customers.Register;

public sealed class RegisterCustomerHandler : IRegisterCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IKeycloakAdminClient _keycloakClient;

    public RegisterCustomerHandler(ICustomerRepository repository, IKeycloakAdminClient keycloakClient)
    {
        _repository = repository;
        _keycloakClient = keycloakClient;
    }

    public async Task<Result<Guid>> HandleAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validationError =
            CustomerRequestValidation.ValidateEmail(request.Email) ??
            CustomerRequestValidation.ValidatePassword(request.Password) ??
            CustomerRequestValidation.ValidateName(request.FirstName, "FirstName") ??
            CustomerRequestValidation.ValidateName(request.LastName, "LastName") ??
            CustomerRequestValidation.ValidatePhoneNumber(request.PhoneNumber);

        if (validationError is not null)
            return validationError;

        var keycloakResult = await _keycloakClient.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (keycloakResult.IsFailure)
            return keycloakResult.Error;

        var keycloakUserId = keycloakResult.Value;

        var customer = CustomerEntity.Create(
            keycloakUserId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        try
        {
            await _repository.AddAsync(customer, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return customer.Id;
        }
        catch
        {
            await _keycloakClient.DeleteUserAsync(keycloakUserId, cancellationToken);
            throw;
        }
    }
}
