using Ecommerce.Backend.Data;
using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Backend.Services;

public class OrderService(AppDbContext db, CartService carts)
{
    /// <summary>
    /// Creates an order from the user's server-side cart. Prices and totals come exclusively
    /// from the database; stock is decremented atomically inside the same transaction.
    /// </summary>
    public async Task<OrderDto> PlaceOrderAsync(int userId, CreateOrderRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var cart = await carts.LoadCartAsync(userId, ct);
        if (cart.Items.Count == 0)
            throw ApiException.BadRequest("Your cart is empty.");

        // Lock rows in a consistent order to avoid deadlocks between concurrent orders.
        foreach (var item in cart.Items.OrderBy(i => i.ProductId))
        {
            var updated = await db.Products
                .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity), ct);

            if (updated == 0)
            {
                var available = await db.Products.Where(p => p.Id == item.ProductId).Select(p => p.Stock).FirstAsync(ct);
                throw ApiException.Conflict(available == 0
                    ? $"\"{item.Product.Name}\" is out of stock. Please remove it from your cart."
                    : $"Only {available} unit(s) of \"{item.Product.Name}\" available. Please update your cart.");
            }
        }

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductImageUrl = i.Product.ImageUrl,
                UnitPrice = i.Product.Price,
                Quantity = i.Quantity,
                LineTotal = i.Product.Price * i.Quantity
            }).ToList()
        };
        order.Total = order.Items.Sum(i => i.LineTotal);

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        db.Entry(cart).State = EntityState.Modified;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToDto(order);
    }

    public Task<List<OrderSummaryDto>> GetOrdersAsync(int userId, CancellationToken ct) =>
        db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id, o.Status.ToString(), o.Total, o.Items.Sum(i => i.Quantity), o.CreatedAt))
            .ToListAsync(ct);

    public async Task<OrderDto> GetOrderAsync(int userId, int orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
                        .Include(o => o.Items.OrderBy(i => i.Id))
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
                    ?? throw ApiException.NotFound("Order not found.");
        return ToDto(order);
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id,
        o.Status.ToString(),
        o.PaymentMethod.ToString(),
        o.CustomerName,
        o.Phone,
        o.DeliveryAddress,
        o.Total,
        o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.ProductImageUrl, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
}
