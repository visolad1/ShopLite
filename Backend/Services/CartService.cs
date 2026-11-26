using Ecommerce.Backend.Data;
using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Backend.Services;

public class CartService(AppDbContext db)
{
    public async Task<CartDto> GetCartAsync(int userId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(userId, ct);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
                      ?? throw ApiException.NotFound("Product not found.");

        var cart = await LoadCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
        var newQuantity = (item?.Quantity ?? 0) + request.Quantity;

        EnsureStock(product, newQuantity);
        if (newQuantity > 99)
            throw ApiException.BadRequest("You can add at most 99 units of a product.");

        if (item is null)
            cart.Items.Add(new CartItem { ProductId = product.Id, Product = product, Quantity = newQuantity });
        else
            item.Quantity = newQuantity;

        db.Entry(cart).State = EntityState.Modified; // bump UpdatedAt
        await db.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task<CartDto> UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct)
    {
        var cart = await LoadCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw ApiException.NotFound("Cart item not found.");

        EnsureStock(item.Product, request.Quantity);
        item.Quantity = request.Quantity;

        db.Entry(cart).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int itemId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw ApiException.NotFound("Cart item not found.");

        cart.Items.Remove(item);
        db.CartItems.Remove(item);
        db.Entry(cart).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    /// <summary>Loads the user's cart with items and products, creating the cart if it is missing.</summary>
    internal async Task<Cart> LoadCartAsync(int userId, CancellationToken ct)
    {
        var cart = await db.Carts
            .Include(c => c.Items.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id))
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null)
            return cart;

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return cart;
    }

    private static void EnsureStock(Product product, int quantity)
    {
        if (product.Stock <= 0)
            throw ApiException.Conflict($"\"{product.Name}\" is out of stock.");
        if (quantity > product.Stock)
            throw ApiException.Conflict($"Only {product.Stock} unit(s) of \"{product.Name}\" available.");
    }

    internal static CartDto ToDto(Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemDto(
                i.Id, i.ProductId, i.Product.Name, i.Product.ImageUrl, i.Product.Price,
                i.Quantity, i.Product.Price * i.Quantity, i.Product.Stock))
            .ToList();

        return new CartDto(items, items.Sum(i => i.Quantity), items.Sum(i => i.LineTotal));
    }
}
