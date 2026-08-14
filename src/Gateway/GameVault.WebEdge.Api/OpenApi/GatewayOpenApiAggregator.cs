using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace GameVault.WebEdge.Api.OpenApi;

internal sealed class GatewayOpenApiAggregator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GatewayOpenApiAggregator> _logger;
    private readonly GatewayOpenApiOptions _options;

    public GatewayOpenApiAggregator(
        IHttpClientFactory httpClientFactory,
        ILogger<GatewayOpenApiAggregator> logger,
        IOptions<GatewayOpenApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<JsonObject> AggregateAsync(CancellationToken cancellationToken)
    {
        var mergedPaths = new JsonObject();
        var mergedSchemas = new JsonObject();

        foreach (var service in _options.Services)
        {
            JsonElement root;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var json = await client.GetStringAsync(service.OpenApiUrl, cancellationToken);
                root = JsonDocument.Parse(json).RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch OpenAPI spec from {Service} at {Url}", service.Name, service.OpenApiUrl);
                continue;
            }

            var allowedPaths = service.AllowedDownstreamPaths is { Count: > 0 }
                ? new HashSet<string>(service.AllowedDownstreamPaths, StringComparer.OrdinalIgnoreCase)
                : null;

            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    if (allowedPaths is not null && !allowedPaths.Contains(path.Name))
                        continue;

                    mergedPaths[service.PathPrefix + path.Name] = RewriteRefs(path.Value, service.Name);
                }
            }

            if (root.TryGetProperty("components", out var components) &&
                components.TryGetProperty("schemas", out var schemas))
            {
                foreach (var schema in schemas.EnumerateObject())
                {
                    mergedSchemas[service.Name + schema.Name] = RewriteRefs(schema.Value, service.Name);
                }
            }
        }

        mergedPaths["/auth/token"] = BuildAuthTokenPath();

        return new JsonObject
        {
            ["openapi"] = "3.0.1",
            ["info"] = new JsonObject
            {
                ["title"] = "GameVault API",
                ["version"] = "1.0"
            },
            ["servers"] = new JsonArray
            {
                new JsonObject { ["url"] = "http://localhost:5000" }
            },
            ["paths"] = mergedPaths,
            ["components"] = new JsonObject { ["schemas"] = mergedSchemas }
        };
    }

    private static JsonObject BuildAuthTokenPath() =>
        new()
        {
            ["post"] = new JsonObject
            {
                ["tags"] = new JsonArray { "Auth" },
                ["summary"] = "Obtain a Keycloak access token (Resource Owner Password flow)",
                ["requestBody"] = new JsonObject
                {
                    ["required"] = true,
                    ["content"] = new JsonObject
                    {
                        ["application/x-www-form-urlencoded"] = new JsonObject
                        {
                            ["schema"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["required"] = new JsonArray { "grant_type", "client_id", "username", "password" },
                                ["properties"] = new JsonObject
                                {
                                    ["grant_type"] = new JsonObject { ["type"] = "string", ["example"] = "password" },
                                    ["client_id"] = new JsonObject { ["type"] = "string" },
                                    ["username"] = new JsonObject { ["type"] = "string" },
                                    ["password"] = new JsonObject { ["type"] = "string", ["format"] = "password" },
                                    ["scope"] = new JsonObject { ["type"] = "string", ["example"] = "openid" }
                                }
                            }
                        }
                    }
                },
                ["responses"] = new JsonObject
                {
                    ["200"] = new JsonObject
                    {
                        ["description"] = "Token issued successfully",
                        ["content"] = new JsonObject
                        {
                            ["application/json"] = new JsonObject
                            {
                                ["schema"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JsonObject
                                    {
                                        ["access_token"] = new JsonObject { ["type"] = "string" },
                                        ["token_type"] = new JsonObject { ["type"] = "string" },
                                        ["expires_in"] = new JsonObject { ["type"] = "integer" },
                                        ["refresh_token"] = new JsonObject { ["type"] = "string" }
                                    }
                                }
                            }
                        }
                    },
                    ["401"] = new JsonObject { ["description"] = "Invalid credentials" }
                }
            }
        };

    private static JsonNode? RewriteRefs(JsonElement element, string servicePrefix) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => RewriteObjectRefs(element, servicePrefix),
            JsonValueKind.Array => RewriteArrayRefs(element, servicePrefix),
            JsonValueKind.String => JsonValue.Create(element.GetString()),
            JsonValueKind.Number => RewriteNumber(element),
            JsonValueKind.True => JsonValue.Create(true),
            JsonValueKind.False => JsonValue.Create(false),
            _ => null
        };

    private static JsonObject RewriteObjectRefs(JsonElement element, string servicePrefix)
    {
        const string schemasBase = "#/components/schemas/";
        var obj = new JsonObject();

        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name == "$ref" &&
                prop.Value.ValueKind == JsonValueKind.String &&
                prop.Value.GetString() is { } refVal &&
                refVal.StartsWith(schemasBase, StringComparison.Ordinal))
            {
                obj["$ref"] = schemasBase + servicePrefix + refVal[schemasBase.Length..];
            }
            else
            {
                obj[prop.Name] = RewriteRefs(prop.Value, servicePrefix);
            }
        }

        return obj;
    }

    private static JsonArray RewriteArrayRefs(JsonElement element, string servicePrefix)
    {
        var arr = new JsonArray();
        foreach (var item in element.EnumerateArray())
            arr.Add(RewriteRefs(item, servicePrefix));
        return arr;
    }

    private static JsonNode RewriteNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var l)) return JsonValue.Create(l);
        if (element.TryGetDouble(out var d)) return JsonValue.Create(d);
        return JsonValue.Create(0);
    }
}
