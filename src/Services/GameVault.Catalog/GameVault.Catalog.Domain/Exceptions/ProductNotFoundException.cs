using GameVault.Common.Exceptions;

namespace GameVault.Catalog.Domain.Exceptions;

public sealed class ProductNotFoundException : NotFoundException
{
    public ProductNotFoundException(Guid id)
        : base($"Product with ID '{id}' was not found.") { }
}
