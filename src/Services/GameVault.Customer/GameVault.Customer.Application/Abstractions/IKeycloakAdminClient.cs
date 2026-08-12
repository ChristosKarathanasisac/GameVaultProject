using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Abstractions;

public interface IKeycloakAdminClient
{
    Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> ReactivateUserAsync(
        Guid userId,
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
