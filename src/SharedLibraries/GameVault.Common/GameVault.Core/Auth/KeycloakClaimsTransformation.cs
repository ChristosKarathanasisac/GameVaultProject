using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace GameVault.Core.Auth;

public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string RolesProperty = "roles";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirstValue(RealmAccessClaim);
        if (realmAccess is null)
            return Task.FromResult(principal);

        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty(RolesProperty, out var rolesElement))
            return Task.FromResult(principal);

        var identity = (ClaimsIdentity)principal.Identity!;

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (roleValue is not null && !identity.HasClaim(ClaimTypes.Role, roleValue))
                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
        }

        return Task.FromResult(principal);
    }
}
