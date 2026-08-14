using Microsoft.Extensions.Options;

namespace GameVault.WebEdge.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/token", HandleTokenAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleTokenAsync(
        HttpContext ctx,
        IOptions<KeycloakOptions> options,
        IHttpClientFactory factory,
        CancellationToken ct)
    {
        var form = await ctx.Request.ReadFormAsync(ct);
        var keycloak = options.Value;
        var grantType = form[AuthConsts.GrantTypeKey].ToString();

        if (grantType != AuthConsts.PasswordGrant && grantType != AuthConsts.RefreshTokenGrant)
            return Results.BadRequest(new { error = "unsupported_grant_type" });

        var tokenForm = new Dictionary<string, string>
        {
            [AuthConsts.GrantTypeKey] = grantType,
            [AuthConsts.ClientIdKey] = keycloak.ClientId,
            [AuthConsts.ClientSecretKey] = keycloak.ClientSecret,
        };

        if (grantType == AuthConsts.PasswordGrant)
        {
            tokenForm[AuthConsts.UsernameKey] = form[AuthConsts.UsernameKey].ToString();
            tokenForm[AuthConsts.PasswordKey] = form[AuthConsts.PasswordKey].ToString();
        }
        else
        {
            tokenForm[AuthConsts.RefreshTokenKey] = form[AuthConsts.RefreshTokenKey].ToString();
        }

        var scopeValue = form[AuthConsts.ScopeKey].ToString();
        if (!string.IsNullOrEmpty(scopeValue))
            tokenForm[AuthConsts.ScopeKey] = scopeValue;

        var client = factory.CreateClient();
        var response = await client.PostAsync(
            $"{keycloak.BaseUrl}/realms/{keycloak.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(tokenForm),
            ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
}
