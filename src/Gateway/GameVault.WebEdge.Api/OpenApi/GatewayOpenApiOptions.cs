namespace GameVault.WebEdge.Api.OpenApi;

internal sealed class GatewayOpenApiOptions
{
    public List<DownstreamServiceConfig> Services { get; init; } = [];
}

internal sealed class DownstreamServiceConfig
{
    public required string Name { get; init; }
    public required string OpenApiUrl { get; init; }
    public string PathPrefix { get; init; } = string.Empty;

    // When set, only downstream paths in this list are included in the merged spec.
    // Use this for services whose YARP routes are explicit rather than catch-all,
    // so that internal-only endpoints (e.g. Catalog reservation routes) are excluded.
    public List<string>? AllowedDownstreamPaths { get; init; }
}
