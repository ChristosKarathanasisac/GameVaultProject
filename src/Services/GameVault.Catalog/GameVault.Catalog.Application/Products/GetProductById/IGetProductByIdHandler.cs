using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Products.GetProductById;

public interface IGetProductByIdHandler
{
    Task<Result<GameResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default);
}
