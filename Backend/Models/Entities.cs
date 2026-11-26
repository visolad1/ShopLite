namespace Ecommerce.Backend.Models;

/// <summary>Common base for all persisted entities.</summary>
public abstract class EntityBase
{
    public int Id { get; set; }

    /// <summary>UTC creation timestamp. Stamped automatically by <c>AppDbContext</c> when left unset.</summary>
    public DateTime CreatedAt { get; set; }
}

public class User : EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Cart? Cart { get; set; }
    public List<Order> Orders { get; set; } = [];
}

public class Category : EntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short emoji used as a lightweight category icon in the mobile app.</summary>
    public string Icon { get; set; } = string.Empty;

    public List<Product> Products { get; set; } = [];
}

public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Stock { get; set; }
    public bool IsFeatured { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public class Cart : EntityBase
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }

    public List<CartItem> Items { get; set; } = [];
}

public class CartItem : EntityBase
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

public enum PaymentMethod
{
    CashOnDelivery
}

public class Order : EntityBase
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>Calculated on the server from product prices at order time.</summary>
    public decimal Total { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem : EntityBase
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>Reference to the product; nulled if the product is ever deleted. Snapshots below keep history intact.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    // Snapshots taken when the order was placed.
    public string ProductName { get; set; } = string.Empty;
    public string ProductImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
