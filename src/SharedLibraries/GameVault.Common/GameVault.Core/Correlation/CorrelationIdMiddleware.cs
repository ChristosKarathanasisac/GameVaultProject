using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace GameVault.Core.Correlation;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdConsts.HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Request.Headers[CorrelationIdConsts.HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConsts.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationIdConsts.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}
