using Ecommerce.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Backend.Data;

/// <summary>
/// Catalog seed data applied through EF Core migrations (HasData).
/// Product photos are free-to-use images from Unsplash (https://unsplash.com/license).
/// </summary>
public static class SeedData
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static string Img(string photoId) =>
        $"https://images.unsplash.com/photo-{photoId}?w=800&h=800&fit=crop&q=80&fm=jpg";

    public static void Apply(ModelBuilder b)
    {
        b.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Icon = "🎧", CreatedAt = SeedDate },
            new Category { Id = 2, Name = "Fashion", Icon = "👕", CreatedAt = SeedDate },
            new Category { Id = 3, Name = "Footwear", Icon = "👟", CreatedAt = SeedDate },
            new Category { Id = 4, Name = "Home & Living", Icon = "🛋️", CreatedAt = SeedDate },
            new Category { Id = 5, Name = "Accessories", Icon = "🎒", CreatedAt = SeedDate });

        b.Entity<Product>().HasData(
            // Electronics
            P(1, 1, "Wireless Over-Ear Headphones", 129.99m, 25, true, "1505740420928-5e560c06d30e",
                "Comfortable over-ear headphones with active noise cancellation, 30-hour battery life and fast USB-C charging."),
            P(2, 1, "Studio Monitor Headphones", 89.00m, 18, false, "1583394838336-acd977736f90",
                "Wired closed-back headphones tuned for accurate, balanced sound. Ideal for mixing, podcasting and focused listening."),
            P(3, 1, "Instant Film Camera", 99.50m, 12, true, "1526170375885-4d8ecf77b99f",
                "Retro-styled instant camera with autofocus lens and built-in flash. Print your memories in seconds."),
            P(4, 1, "Ultrabook Laptop 14\"", 1099.00m, 7, false, "1496181133206-80ce9b88a853",
                "Thin and light 14-inch laptop with 16 GB RAM, 512 GB SSD and an all-day battery for work and study."),
            P(5, 1, "Smartphone 6.1\"", 699.00m, 15, false, "1511707171634-5f897ff02aa9",
                "6.1-inch OLED display, dual camera system and 128 GB of storage in a durable aluminium frame."),
            P(6, 1, "Smart Watch Series S", 249.00m, 20, true, "1546868871-7041f2a55e12",
                "Track workouts, heart rate and sleep. Water resistant to 50 m with up to 2 days of battery life."),

            // Fashion
            P(7, 2, "Classic White T-Shirt", 19.99m, 60, false, "1521572163474-6864f9cf17ab",
                "Soft 100% organic cotton tee with a relaxed fit. A wardrobe essential that goes with everything."),
            P(8, 2, "Leather Biker Jacket", 189.00m, 8, true, "1551028719-00167b16eac5",
                "Genuine leather biker jacket with asymmetric zip, quilted lining and three zip pockets."),
            P(9, 2, "Knit Poncho Sweater", 49.00m, 14, false, "1434389677669-e08b4cac3105",
                "Lightweight open-knit poncho with fringe trim. Perfect for layering on cool evenings."),
            P(10, 2, "Black Graphic Tee", 24.00m, 40, false, "1618354691373-d851c5c3a990",
                "Heavyweight cotton t-shirt with a small chest print. Pre-shrunk and garment dyed."),

            // Footwear
            P(11, 3, "Everyday Running Sneakers", 119.00m, 22, true, "1600185365483-26d7a4cc7519",
                "Cushioned running shoes with breathable mesh upper and a responsive foam midsole."),
            P(12, 3, "Lightweight Trainers", 95.00m, 16, false, "1491553895911-0055eca6402d",
                "Minimal knit trainers that weigh next to nothing. Great for the gym or daily walks."),
            P(13, 3, "Suede Derby Shoes", 139.00m, 0, false, "1560343090-f0409e92791a",
                "Hand-finished suede derby shoes with a cushioned leather insole and durable rubber sole."),

            // Home & Living
            P(14, 4, "Ceramic Coffee Mug", 14.00m, 80, false, "1514228742587-6b1558fcca3d",
                "Matte white stoneware mug, 350 ml. Dishwasher and microwave safe."),
            P(15, 4, "Adjustable Desk Lamp", 45.00m, 30, false, "1507473885765-e6ed057f782c",
                "Industrial-style metal desk lamp with an adjustable arm and warm LED bulb included."),
            P(16, 4, "Wooden Bar Stool", 79.00m, 10, false, "1503602642458-232111445657",
                "Solid wood bar stool with a footrest. Seat height 65 cm, suitable for kitchen counters."),
            P(17, 4, "Velvet 3-Seat Sofa", 899.00m, 3, true, "1555041469-a586c61ea9bc",
                "Mid-century three-seat sofa upholstered in soft emerald velvet with solid oak legs."),
            P(18, 4, "Insulated Water Bottle", 29.00m, 50, false, "1602143407151-7111542de6e8",
                "Double-wall stainless steel bottle, 750 ml. Keeps drinks cold for 24 hours or hot for 12."),
            P(24, 4, "Modern Accent Chair", 129.00m, 9, false, "1592078615290-033ee584e267",
                "Moulded seat with a soft cushion and natural beech legs. Fits any dining table or reading nook."),

            // Accessories
            P(19, 5, "Classic Sunglasses", 59.00m, 35, false, "1572635196237-14b3f281503f",
                "Timeless square-frame sunglasses with polarized UV400 lenses."),
            P(20, 5, "Round Metal Sunglasses", 69.00m, 25, false, "1511499767150-a48a237f0083",
                "Lightweight gold-tone round frames with green-tinted polarized lenses."),
            P(21, 5, "Everyday Backpack", 79.00m, 28, true, "1553062407-98eeb64c6a62",
                "Water-resistant 20 L backpack with a padded 15\" laptop sleeve and quick-access pocket."),
            P(22, 5, "Minimalist Watch", 149.00m, 12, false, "1523275335684-37898b6baf30",
                "Clean dial, sapphire-coated glass and a soft silicone strap. Water resistant to 30 m."),
            P(23, 5, "Canvas Tote Bag", 22.00m, 45, false, "1544816155-12df9643f363",
                "Sturdy natural canvas tote with long handles. Fits a laptop, groceries and more."));
    }

    private static Product P(int id, int categoryId, string name, decimal price, int stock, bool featured, string photoId, string description) =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            IsFeatured = featured,
            ImageUrl = Img(photoId),
            CreatedAt = SeedDate
        };
}
