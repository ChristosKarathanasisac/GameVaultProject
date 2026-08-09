using GameVault.Contracts.Responses.Customer;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers.GetById;

public interface IGetCustomerByIdHandler
{
    Task<Result<CustomerResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default);
}
