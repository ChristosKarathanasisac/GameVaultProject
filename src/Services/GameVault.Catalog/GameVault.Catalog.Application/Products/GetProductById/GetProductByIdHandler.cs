using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Domain.Exceptions;
using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.Application.Products.GetProductById;

public sealed class GetProductByIdHandler : IGetProductByIdHandler
{
    private readonly IProductRepository _repository;

    public GetProductByIdHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<GameResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);

        if (product is null)
            throw new ProductNotFoundException(id);

        return product.ToGameResponse();
    }
}
