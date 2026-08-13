using GameVault.Core.Extensions;
using GameVault.WebEdge.Api.Auth;
using GameVault.WebEdge.Api.OpenApi;
using Serilog;

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

    builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
    builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddCorrelationId();
    builder.Services.AddHttpClient();
    builder.Services.Configure<GatewayOpenApiOptions>(builder.Configuration.GetSection("OpenApiAggregation"));
    builder.Services.AddSingleton<GatewayOpenApiAggregator>();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/openapi/v1.json", async (GatewayOpenApiAggregator aggregator, CancellationToken ct) =>
        {
            var doc = await aggregator.AggregateAsync(ct);
            return Results.Json(doc);
        }).ExcludeFromDescription();
    }

    app.UseCorrelationId();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapAuthEndpoints();
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
