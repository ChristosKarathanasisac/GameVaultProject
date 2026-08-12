using GameVault.Contracts.Requests.Customer;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.Reactivate;

public interface IReactivateCustomerHandler
{
    Task<Result<Unit>> HandleAsync(ReactivateCustomerRequest request, CancellationToken cancellationToken = default);
}
