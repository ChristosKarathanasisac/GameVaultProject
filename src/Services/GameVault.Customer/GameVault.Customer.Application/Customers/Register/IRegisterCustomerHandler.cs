using GameVault.Contracts.Requests.Customer;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.Register;

public interface IRegisterCustomerHandler
{
    Task<Result<Guid>> HandleAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
}
