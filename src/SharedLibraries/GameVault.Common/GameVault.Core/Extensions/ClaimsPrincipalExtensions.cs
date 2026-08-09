using System.Security.Claims;

namespace GameVault.Core.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string SubClaimType = "sub";

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(SubClaimType)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
