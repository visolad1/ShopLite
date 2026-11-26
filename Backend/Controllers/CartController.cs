using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Backend.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController(CartService carts) : ControllerBase
{
    [HttpGet]
    public Task<CartDto> Get(CancellationToken ct) => carts.GetCartAsync(User.GetUserId(), ct);

    /// <summary>Adds a product to the cart (or increases its quantity if already present).</summary>
    [HttpPost("items")]
    public Task<CartDto> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        carts.AddItemAsync(User.GetUserId(), request, ct);

    /// <summary>Sets the quantity of a cart item.</summary>
    [HttpPatch("items/{id:int}")]
    public Task<CartDto> UpdateItem(int id, UpdateCartItemRequest request, CancellationToken ct) =>
        carts.UpdateItemAsync(User.GetUserId(), id, request, ct);

    [HttpDelete("items/{id:int}")]
    public Task<CartDto> RemoveItem(int id, CancellationToken ct) =>
        carts.RemoveItemAsync(User.GetUserId(), id, ct);
}
