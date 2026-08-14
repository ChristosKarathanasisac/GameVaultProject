using GameVault.Catalog.Application.Products.GetProductById;
using GameVault.Catalog.Application.Products.ListProducts;
using GameVault.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GameVault.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IGetProductByIdHandler _getByIdHandler;
    private readonly IListProductsHandler _listProductsHandler;

    public ProductsController(IGetProductByIdHandler getByIdHandler, IListProductsHandler listProductsHandler)
    {
        _getByIdHandler = getByIdHandler;
        _listProductsHandler = listProductsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _listProductsHandler.HandleAsync(new ListProductsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(id, cancellationToken);
        return result.ToActionResult(Ok);
    }
}
