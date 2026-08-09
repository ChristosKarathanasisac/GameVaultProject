using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Errors;

public static class CustomerErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Customer.NotFound", "The customer was not found.");

    public static readonly Error Forbidden =
        Error.Forbidden("Customer.Forbidden", "You are not authorized to modify this resource.");

    public static readonly Error EmailConflict =
        Error.Conflict("Customer.EmailConflict", "A customer with this email already exists.");
}
