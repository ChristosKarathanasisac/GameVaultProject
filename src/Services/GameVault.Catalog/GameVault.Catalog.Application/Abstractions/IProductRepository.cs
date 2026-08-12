using GameVault.Catalog.Domain.Entities;

namespace GameVault.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
