namespace GameVault.Contracts.Requests.Customer;

public sealed record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);
