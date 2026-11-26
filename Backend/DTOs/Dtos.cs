using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Backend.DTOs;

// ---------- Auth ----------

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters long."), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public record UserDto(int Id, string Email, string FullName, DateTime CreatedAt);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

// ---------- Catalog ----------

public record CategoryDto(int Id, string Name, string Icon, int ProductCount);

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    int Stock,
    bool InStock,
    bool IsFeatured,
    int CategoryId,
    string CategoryName);

// ---------- Cart ----------

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 99)]
    public int Quantity { get; set; }
}

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int Stock);

public record CartDto(List<CartItemDto> Items, int TotalQuantity, decimal Total);

// ---------- Orders ----------

public class CreateOrderRequest
{
    [Required, MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Please enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(5), MaxLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;
}

public record OrderItemDto(
    int? ProductId,
    string ProductName,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record OrderSummaryDto(int Id, string Status, decimal Total, int ItemCount, DateTime CreatedAt);

public record OrderDto(
    int Id,
    string Status,
    string PaymentMethod,
    string CustomerName,
    string Phone,
    string DeliveryAddress,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItemDto> Items);
