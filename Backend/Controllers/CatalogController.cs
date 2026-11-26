using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Backend.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public class CatalogController(CatalogService catalog) : ControllerBase
{
    /// <summary>Lists products, optionally filtered by search text, category and featured flag.</summary>
    [HttpGet("products")]
    public Task<List<ProductDto>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool? featured,
        CancellationToken ct) => catalog.GetProductsAsync(search, categoryId, featured, ct);

    [HttpGet("products/{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProductDto> GetProduct(int id, CancellationToken ct) => catalog.GetProductAsync(id, ct);

    [HttpGet("categories")]
    public Task<List<CategoryDto>> GetCategories(CancellationToken ct) => catalog.GetCategoriesAsync(ct);
}
