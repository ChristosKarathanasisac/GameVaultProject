using GameVault.Contracts.Requests.Customer;
using GameVault.Core.Extensions;
using GameVault.Customer.Application.Customers.Delete;
using GameVault.Customer.Application.Customers.GetById;
using GameVault.Customer.Application.Customers.Register;
using GameVault.Customer.Application.Customers.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Customer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomersController : ControllerBase
{
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
        return result.ToActionResult(id => CreatedAtAction(nameof(GetById), new { id }, null));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(id, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
            return Unauthorized();

        var result = await _getByIdHandler.HandleAsync(callerId.Value, cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
            return Unauthorized();

        var result = await _updateHandler.HandleAsync(id, callerId.Value, request, cancellationToken);
        return result.ToActionResult(_ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
            return Unauthorized();

        var result = await _deleteHandler.HandleAsync(id, callerId.Value, cancellationToken);
        return result.ToActionResult(_ => NoContent());
    }
}
