using GameVault.Catalog.Application;
using GameVault.Catalog.Infrastructure;
using GameVault.Catalog.Infrastructure.Persistence;
using GameVault.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Catalog API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    builder.Services.AddControllers();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddExceptionHandling();
    builder.Services.AddCorrelationId();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();
        await CatalogDataSeeder.SeedAsync(db);
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseExceptionHandling();
    app.UseCorrelationId();
    app.UseSerilogRequestLogging();

    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Catalog API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
