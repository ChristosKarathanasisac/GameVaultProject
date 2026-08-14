using GameVault.Order.Domain.Enums;
using GameVault.SharedKernel.Exceptions;

namespace GameVault.Order.Domain.Exceptions;

public sealed class InvalidOrderStatusException : DomainException
{
    public InvalidOrderStatusException(OrderStatus current, string operation)
        : base($"Cannot perform '{operation}' on an order with status '{current}'.") { }
}
