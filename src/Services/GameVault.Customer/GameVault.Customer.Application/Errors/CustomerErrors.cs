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

    public static readonly Error ReactivationConflict =
        Error.Conflict("Customer.ReactivationConflict", "This account could not be reactivated. Please contact support.");

    public static readonly Error InvalidEmail =
        Error.Validation("Customer.Validation.InvalidEmail", "Email is not a valid email address.");

    public static Error Required(string field) =>
        Error.Validation("Customer.Validation.Required", $"{field} is required.");

    public static Error TooLong(string field, int maxLength) =>
        Error.Validation("Customer.Validation.TooLong", $"{field} must not exceed {maxLength} characters.");

    public static Error WeakPassword(int minLength) =>
        Error.Validation("Customer.Validation.WeakPassword", $"Password must be at least {minLength} characters long.");

    public static Error RegistrationRejectedByKeycloak(string detail) =>
        Error.Validation("Customer.Validation.KeycloakRejected", detail);
}
