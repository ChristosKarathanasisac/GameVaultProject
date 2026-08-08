using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.Application.Products.GetProductById;

public interface IGetProductByIdHandler
{
    Task<GameResponse?> HandleAsync(Guid id, CancellationToken cancellationToken = default);
}
