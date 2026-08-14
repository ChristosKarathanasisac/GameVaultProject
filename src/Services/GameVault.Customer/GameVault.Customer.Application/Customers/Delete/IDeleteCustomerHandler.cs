using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.Delete;

public interface IDeleteCustomerHandler
{
    Task<Result<Unit>> HandleAsync(Guid routeId, Guid callerId, CancellationToken cancellationToken = default);
}
