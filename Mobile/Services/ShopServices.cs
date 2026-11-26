using Ecommerce.Mobile.Models;

namespace Ecommerce.Mobile.Services;

public class ProductService(ApiClient api)
{
    public Task<List<Product>> GetProductsAsync(string? search = null, int? categoryId = null, bool? featured = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (categoryId is not null) query.Add("categoryId=" + categoryId);
        if (featured is not null) query.Add("featured=" + (featured.Value ? "true" : "false"));

        var path = "api/products" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return api.GetAsync<List<Product>>(path, ct);
    }

    public Task<Product> GetProductAsync(int id, CancellationToken ct = default) =>
        api.GetAsync<Product>($"api/products/{id}", ct);

    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        api.GetAsync<List<Category>>("api/categories", ct);
}

/// <summary>Cart operations. Every call returns the full, server-calculated cart.</summary>
public class CartService(ApiClient api)
{
    /// <summary>Raised whenever the cart changes so other screens can refresh.</summary>
    public event EventHandler<Cart>? CartChanged;

    public Task<Cart> GetCartAsync(CancellationToken ct = default) => api.GetAsync<Cart>("api/cart", ct);

    public Task<Cart> AddAsync(int productId, int quantity) =>
        Notify(api.PostAsync<Cart>("api/cart/items", new AddCartItemRequest(productId, quantity)));

    public Task<Cart> UpdateQuantityAsync(int itemId, int quantity) =>
        Notify(api.PatchAsync<Cart>($"api/cart/items/{itemId}", new UpdateCartItemRequest(quantity)));

    public Task<Cart> RemoveAsync(int itemId) =>
        Notify(api.DeleteAsync<Cart>($"api/cart/items/{itemId}"));

    public void NotifyCleared() => CartChanged?.Invoke(this, Cart.Empty);

    private async Task<Cart> Notify(Task<Cart> call)
    {
        var cart = await call;
        CartChanged?.Invoke(this, cart);
        return cart;
    }
}

public class OrderService(ApiClient api, CartService cart)
{
    public async Task<Order> PlaceOrderAsync(CreateOrderRequest request)
    {
        var order = await api.PostAsync<Order>("api/orders", request);
        cart.NotifyCleared(); // the server empties the cart after a successful order
        return order;
    }

    public Task<List<OrderSummary>> GetOrdersAsync(CancellationToken ct = default) =>
        api.GetAsync<List<OrderSummary>>("api/orders", ct);

    public Task<Order> GetOrderAsync(int id, CancellationToken ct = default) =>
        api.GetAsync<Order>($"api/orders/{id}", ct);
}

/// <summary>Thin wrapper over Shell navigation, alerts and toasts so view models stay UI-agnostic.</summary>
public interface IUiService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task AlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Cancel");
    Task ToastAsync(string message);
}

public class UiService : IUiService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        MainThread.InvokeOnMainThreadAsync(() => parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters));

    public Task AlertAsync(string title, string message, string cancel = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, cancel));

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Cancel") =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, accept, cancel));

    public Task ToastAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() =>
            CommunityToolkit.Maui.Alerts.Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Short).Show());
}
