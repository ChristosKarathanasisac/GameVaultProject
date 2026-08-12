using GameVault.Catalog.Domain.Entities;
using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.Application.Products;

internal static class ProductMapper
{
    internal static GameResponse ToGameResponse(this Product product) =>
        new(product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock?.AvailableStock ?? 0);
}
