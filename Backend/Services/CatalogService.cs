using Ecommerce.Backend.Data;
using Ecommerce.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Backend.Services;

public class CatalogService(AppDbContext db)
{
    public async Task<List<ProductDto>> GetProductsAsync(string? search, int? categoryId, bool? featured, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100) term = term[..100];
            // Escape LIKE wildcards so user input is matched literally.
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\") ||
                EF.Functions.ILike(p.Description, pattern, "\\") ||
                EF.Functions.ILike(p.Category.Name, pattern, "\\"));
        }

        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        if (featured is not null)
            query = query.Where(p => p.IsFeatured == featured);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.Stock, p.Stock > 0,
                p.IsFeatured, p.CategoryId, p.Category.Name))
            .ToListAsync(ct);
    }

    public async Task<ProductDto> GetProductAsync(int id, CancellationToken ct)
    {
        return await db.Products.AsNoTracking()
                   .Where(p => p.Id == id)
                   .Select(p => new ProductDto(
                       p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.Stock, p.Stock > 0,
                       p.IsFeatured, p.CategoryId, p.Category.Name))
                   .FirstOrDefaultAsync(ct)
               ?? throw ApiException.NotFound("Product not found.");
    }

    public Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct) =>
        db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon, c.Products.Count))
            .ToListAsync(ct);
}
