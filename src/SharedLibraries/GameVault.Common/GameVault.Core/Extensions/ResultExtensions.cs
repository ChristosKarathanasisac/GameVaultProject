using GameVault.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Core.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        var problem = new ProblemDetails
        {
            Title = result.Error.Description,
            Detail = result.Error.Code
        };

        return result.Error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(problem),
            ErrorType.Forbidden => new ObjectResult(problem) { StatusCode = StatusCodes.Status403Forbidden },
            ErrorType.Conflict => new ConflictObjectResult(problem),
            _ => new ObjectResult(problem) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
