using GameVault.Core.Extensions;
using GameVault.Notifications.Application;
using GameVault.Notifications.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Notifications API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    builder.Services.AddControllers().AddDapr();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    builder.Services.AddExceptionHandling();
    builder.Services.AddCorrelationId();

    var app = builder.Build();

    app.UseExceptionHandling();
    app.UseCorrelationId();
    app.UseSerilogRequestLogging();

    app.UseCloudEvents();
    app.MapSubscribeHandler();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notifications API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
