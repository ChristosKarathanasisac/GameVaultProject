using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}
