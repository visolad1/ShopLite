using CommunityToolkit.Mvvm.ComponentModel;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.Models;

// ---------- Auth ----------

public record User(int Id, string Email, string FullName, DateTime CreatedAt);

public record AuthResponse(string Token, DateTime ExpiresAt, User User);

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName);

// ---------- Catalog ----------

public record Category(int Id, string Name, string Icon, int ProductCount);

public record Product(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    int Stock,
    bool InStock,
    bool IsFeatured,
    int CategoryId,
    string CategoryName)
{
    /// <summary>400px image for grid cards (full size is used on the details page).</summary>
    public string ThumbnailUrl => ImageUrls.Resize(ImageUrl, 400);
}

/// <summary>A selectable category chip (includes the synthetic "All" chip).</summary>
public partial class CategoryChip(int? id, string name, string icon) : ObservableObject
{
    public int? Id { get; } = id;
    public string Name { get; } = name;
    public string Icon { get; } = icon;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

// ---------- Cart ----------

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int Stock);

public record Cart(List<CartItemDto> Items, int TotalQuantity, decimal Total)
{
    public static Cart Empty { get; } = new([], 0, 0);
}

public record AddCartItemRequest(int ProductId, int Quantity);

public record UpdateCartItemRequest(int Quantity);

/// <summary>Observable cart line so quantity changes update in place without rebuilding the list.</summary>
public partial class CartLine : ObservableObject
{
    public CartLine(CartItemDto dto) => Update(dto);

    public int Id { get; private set; }
    public int ProductId { get; private set; }

    [ObservableProperty] public partial string ProductName { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailUrl))]
    public partial string ImageUrl { get; set; } = string.Empty;

    public string ThumbnailUrl => ImageUrls.Resize(ImageUrl, 200);
    [ObservableProperty] public partial decimal UnitPrice { get; set; }
    [ObservableProperty] public partial int Stock { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIncrease))]
    [NotifyPropertyChangedFor(nameof(CanDecrease))]
    public partial int Quantity { get; set; }

    [ObservableProperty] public partial decimal LineTotal { get; set; }

    public bool CanIncrease => Quantity < Math.Min(Stock, 99);
    public bool CanDecrease => Quantity > 1;

    public void Update(CartItemDto dto)
    {
        Id = dto.Id;
        ProductId = dto.ProductId;
        ProductName = dto.ProductName;
        ImageUrl = dto.ImageUrl;
        UnitPrice = dto.UnitPrice;
        Stock = dto.Stock;
        Quantity = dto.Quantity;
        LineTotal = dto.LineTotal;
        OnPropertyChanged(nameof(CanIncrease));
    }
}

// ---------- Orders ----------

public record CreateOrderRequest(string CustomerName, string Phone, string DeliveryAddress);

public record OrderItem(int? ProductId, string ProductName, string ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public string ThumbnailUrl => ImageUrls.Resize(ImageUrl, 200);
}

public record OrderSummary(int Id, string Status, decimal Total, int ItemCount, DateTime CreatedAt)
{
    public string Number => $"Order #{Id}";
    public DateTime CreatedAtLocal => CreatedAt.ToLocalTime();
}

public record Order(
    int Id,
    string Status,
    string PaymentMethod,
    string CustomerName,
    string Phone,
    string DeliveryAddress,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItem> Items)
{
    public string Number => $"Order #{Id}";
    public DateTime CreatedAtLocal => CreatedAt.ToLocalTime();
    public string PaymentMethodDisplay => PaymentMethod == "CashOnDelivery" ? "Cash on delivery" : PaymentMethod;
}
