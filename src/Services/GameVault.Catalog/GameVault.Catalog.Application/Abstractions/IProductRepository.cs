using GameVault.Catalog.Domain.Entities;

namespace GameVault.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
