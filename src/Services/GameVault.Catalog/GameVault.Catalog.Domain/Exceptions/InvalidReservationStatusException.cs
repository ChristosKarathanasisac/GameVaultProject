using GameVault.Catalog.Domain.Enums;
using GameVault.SharedKernel.Exceptions;

namespace GameVault.Catalog.Domain.Exceptions;

public sealed class InvalidReservationStatusException : DomainException
{
    public InvalidReservationStatusException(ReservationStatus current, string operation)
        : base($"Cannot perform '{operation}' on a reservation with status '{current}'.") { }
}
