namespace Ecommerce.Mobile.Services;

/// <summary>Shell routes used across the app.</summary>
public static class Routes
{
    // Absolute routes (top-level Shell items / tabs)
    public const string Login = "//login";
    public const string Home = "//main/home";
    public const string Catalog = "//main/catalog";
    public const string Cart = "//main/cart";
    public const string Orders = "//main/orders";
    public const string Profile = "//main/profile";

    // Pushed pages (registered with Routing.RegisterRoute)
    public const string Register = "register";
    public const string ProductDetails = "product";
    public const string Checkout = "checkout";
    public const string OrderDetails = "orderdetails";
}
