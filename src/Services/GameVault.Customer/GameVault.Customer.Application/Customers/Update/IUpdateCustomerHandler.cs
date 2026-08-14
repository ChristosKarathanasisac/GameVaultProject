using GameVault.Contracts.Requests.Customer;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.Update;

public interface IUpdateCustomerHandler
{
    Task<Result<Unit>> HandleAsync(Guid routeId, Guid callerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
}
