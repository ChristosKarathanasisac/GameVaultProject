using GameVault.Core.Extensions;
using Serilog;
using Yarp.ReverseProxy.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WebEdge API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
    var keycloakRealm = builder.Configuration["Keycloak:Realm"];

    builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddCorrelationId();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .LoadFromMemory(
            routes:
            [
                new RouteConfig
                {
                    RouteId = "keycloak-token-route",
                    ClusterId = "keycloak-cluster",
                    AuthorizationPolicy = "Anonymous",
                    Match = new RouteMatch { Path = "/auth/token" },
                    Transforms =
                    [
                        new Dictionary<string, string>
                        {
                            ["PathSet"] = $"/realms/{keycloakRealm}/protocol/openid-connect/token"
                        }
                    ]
                }
            ],
            clusters:
            [
                new ClusterConfig
                {
                    ClusterId = "keycloak-cluster",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["destination1"] = new DestinationConfig { Address = keycloakBaseUrl! }
                    }
                }
            ]);

    var app = builder.Build();

    app.UseCorrelationId();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "WebEdge API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
