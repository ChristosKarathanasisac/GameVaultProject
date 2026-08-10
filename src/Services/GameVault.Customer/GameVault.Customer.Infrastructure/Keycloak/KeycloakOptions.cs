namespace GameVault.Customer.Infrastructure.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Realm { get; init; } = string.Empty;
}
