namespace GameVault.Contracts.Responses.Customer;

public sealed record CustomerResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string RegistrationStatus);
