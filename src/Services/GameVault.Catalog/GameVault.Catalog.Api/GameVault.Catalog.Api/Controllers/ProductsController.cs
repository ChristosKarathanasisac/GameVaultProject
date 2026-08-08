using GameVault.Catalog.Application.Products.GetProductById;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IGetProductByIdHandler _getProductByIdHandler;

    public ProductsController(IGetProductByIdHandler getProductByIdHandler)
    {
        _getProductByIdHandler = getProductByIdHandler;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getProductByIdHandler.HandleAsync(id, cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
