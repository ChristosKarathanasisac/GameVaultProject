namespace GameVault.Customer.Infrastructure.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";
    public const string EndpointName = "keycloak";

    public string Realm { get; init; } = string.Empty;
}
