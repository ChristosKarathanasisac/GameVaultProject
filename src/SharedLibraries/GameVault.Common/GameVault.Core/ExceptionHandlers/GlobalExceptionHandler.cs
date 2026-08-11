using GameVault.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameVault.Core.ExceptionHandlers;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string ConcurrencyConflictDetail =
        "The resource was modified by another request. Reload and try again.";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", ConcurrencyConflictDetail),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", exception.Message)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Domain exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = detail
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
