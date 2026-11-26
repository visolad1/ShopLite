using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ecommerce.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
                    table.CheckConstraint("CK_Products_Stock_NonNegative", "\"Stock\" >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.CheckConstraint("CK_CartItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_OrderItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🎧", "Electronics" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "👕", "Fashion" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "👟", "Footwear" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🛋️", "Home & Living" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🎒", "Accessories" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsFeatured", "Name", "Price", "Stock" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comfortable over-ear headphones with active noise cancellation, 30-hour battery life and fast USB-C charging.", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Wireless Over-Ear Headphones", 129.99m, 25 },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Wired closed-back headphones tuned for accurate, balanced sound. Ideal for mixing, podcasting and focused listening.", "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Studio Monitor Headphones", 89.00m, 18 },
                    { 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Retro-styled instant camera with autofocus lens and built-in flash. Print your memories in seconds.", "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Instant Film Camera", 99.50m, 12 },
                    { 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thin and light 14-inch laptop with 16 GB RAM, 512 GB SSD and an all-day battery for work and study.", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Ultrabook Laptop 14\"", 1099.00m, 7 },
                    { 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "6.1-inch OLED display, dual camera system and 128 GB of storage in a durable aluminium frame.", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Smartphone 6.1\"", 699.00m, 15 },
                    { 6, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Track workouts, heart rate and sleep. Water resistant to 50 m with up to 2 days of battery life.", "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Smart Watch Series S", 249.00m, 20 },
                    { 7, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Soft 100% organic cotton tee with a relaxed fit. A wardrobe essential that goes with everything.", "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Classic White T-Shirt", 19.99m, 60 },
                    { 8, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Genuine leather biker jacket with asymmetric zip, quilted lining and three zip pockets.", "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Leather Biker Jacket", 189.00m, 8 },
                    { 9, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lightweight open-knit poncho with fringe trim. Perfect for layering on cool evenings.", "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Knit Poncho Sweater", 49.00m, 14 },
                    { 10, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Heavyweight cotton t-shirt with a small chest print. Pre-shrunk and garment dyed.", "https://images.unsplash.com/photo-1618354691373-d851c5c3a990?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Black Graphic Tee", 24.00m, 40 },
                    { 11, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cushioned running shoes with breathable mesh upper and a responsive foam midsole.", "https://images.unsplash.com/photo-1600185365483-26d7a4cc7519?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Everyday Running Sneakers", 119.00m, 22 },
                    { 12, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Minimal knit trainers that weigh next to nothing. Great for the gym or daily walks.", "https://images.unsplash.com/photo-1491553895911-0055eca6402d?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Lightweight Trainers", 95.00m, 16 },
                    { 13, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hand-finished suede derby shoes with a cushioned leather insole and durable rubber sole.", "https://images.unsplash.com/photo-1560343090-f0409e92791a?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Suede Derby Shoes", 139.00m, 0 },
                    { 14, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Matte white stoneware mug, 350 ml. Dishwasher and microwave safe.", "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Ceramic Coffee Mug", 14.00m, 80 },
                    { 15, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Industrial-style metal desk lamp with an adjustable arm and warm LED bulb included.", "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Adjustable Desk Lamp", 45.00m, 30 },
                    { 16, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Solid wood bar stool with a footrest. Seat height 65 cm, suitable for kitchen counters.", "https://images.unsplash.com/photo-1503602642458-232111445657?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Wooden Bar Stool", 79.00m, 10 },
                    { 17, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mid-century three-seat sofa upholstered in soft emerald velvet with solid oak legs.", "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Velvet 3-Seat Sofa", 899.00m, 3 },
                    { 18, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Double-wall stainless steel bottle, 750 ml. Keeps drinks cold for 24 hours or hot for 12.", "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Insulated Water Bottle", 29.00m, 50 },
                    { 19, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Timeless square-frame sunglasses with polarized UV400 lenses.", "https://images.unsplash.com/photo-1572635196237-14b3f281503f?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Classic Sunglasses", 59.00m, 35 },
                    { 20, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lightweight gold-tone round frames with green-tinted polarized lenses.", "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Round Metal Sunglasses", 69.00m, 25 },
                    { 21, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Water-resistant 20 L backpack with a padded 15\" laptop sleeve and quick-access pocket.", "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&h=800&fit=crop&q=80&fm=jpg", true, "Everyday Backpack", 79.00m, 28 },
                    { 22, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clean dial, sapphire-coated glass and a soft silicone strap. Water resistant to 30 m.", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Minimalist Watch", 149.00m, 12 },
                    { 23, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sturdy natural canvas tote with long handles. Fits a laptop, groceries and more.", "https://images.unsplash.com/photo-1544816155-12df9643f363?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Canvas Tote Bag", 22.00m, 45 },
                    { 24, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moulded seat with a soft cushion and natural beech legs. Fits any dining table or reading nook.", "https://images.unsplash.com/photo-1592078615290-033ee584e267?w=800&h=800&fit=crop&q=80&fm=jpg", false, "Modern Accent Chair", 129.00m, 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
