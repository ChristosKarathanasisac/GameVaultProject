using GameVault.Catalog.Application.Abstractions;
using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Products.ListProducts;

public sealed class ListProductsHandler : IListProductsHandler
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IProductRepository _repository;

    public ListProductsHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResponse<GameResponse>>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? DefaultPageSize
            : query.PageSize > MaxPageSize ? MaxPageSize
            : query.PageSize;

        var (items, totalCount) = await _repository.ListAsync(page, pageSize, cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<GameResponse>(
            items.Select(p => p.ToGameResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            totalPages);
    }
}
