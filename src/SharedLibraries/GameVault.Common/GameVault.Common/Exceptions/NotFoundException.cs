namespace GameVault.Common.Exceptions;

public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message) : base(message) { }
}
