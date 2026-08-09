namespace GameVault.SharedKernel.Exceptions;

public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message) : base(message) { }
}
