using GameVault.SharedKernel.Exceptions;

namespace GameVault.Catalog.Domain.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Product '{productId}' has only {available} units available but {requested} were requested.") { }
}
