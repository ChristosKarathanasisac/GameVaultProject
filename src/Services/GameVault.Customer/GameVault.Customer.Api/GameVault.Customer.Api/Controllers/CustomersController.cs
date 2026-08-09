using System.Security.Claims;
using GameVault.Contracts.Requests.Customer;
using GameVault.Customer.Application.Customers.Delete;
using GameVault.Customer.Application.Customers.GetById;
using GameVault.Customer.Application.Customers.Register;
using GameVault.Customer.Application.Customers.Update;
using GameVault.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Customer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomersController : ControllerBase
{
    private const string SubClaimType = "sub";

    private readonly IRegisterCustomerHandler _registerHandler;
    private readonly IGetCustomerByIdHandler _getByIdHandler;
    private readonly IUpdateCustomerHandler _updateHandler;
    private readonly IDeleteCustomerHandler _deleteHandler;

    public CustomersController(
        IRegisterCustomerHandler registerHandler,
        IGetCustomerByIdHandler getByIdHandler,
        IUpdateCustomerHandler updateHandler,
        IDeleteCustomerHandler deleteHandler)
    {
        _registerHandler = registerHandler;
        _getByIdHandler = getByIdHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(request, cancellationToken);

        if (result.IsFailure)
            return MapError(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, null);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(id, cancellationToken);

        if (result.IsFailure)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var callerId = GetCallerId();
        if (callerId is null)
            return Unauthorized();

        var result = await _getByIdHandler.HandleAsync(callerId.Value, cancellationToken);

        if (result.IsFailure)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var callerId = GetCallerId();
        if (callerId is null)
            return Unauthorized();

        var result = await _updateHandler.HandleAsync(id, callerId.Value, request, cancellationToken);

        if (result.IsFailure)
            return MapError(result.Error);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var callerId = GetCallerId();
        if (callerId is null)
            return Unauthorized();

        var result = await _deleteHandler.HandleAsync(id, callerId.Value, cancellationToken);

        if (result.IsFailure)
            return MapError(result.Error);

        return NoContent();
    }

    private Guid? GetCallerId()
    {
        var sub = User.FindFirstValue(SubClaimType)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(new ProblemDetails
        {
            Title = error.Description,
            Detail = error.Code
        }),
        ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = error.Description,
            Detail = error.Code
        }),
        ErrorType.Conflict => Conflict(new ProblemDetails
        {
            Title = error.Description,
            Detail = error.Code
        }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        {
            Title = error.Description,
            Detail = error.Code
        })
    };
}
