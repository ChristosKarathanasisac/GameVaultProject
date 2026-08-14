using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Products.ListProducts;

public interface IListProductsHandler
{
    Task<Result<PagedResponse<GameResponse>>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default);
}
