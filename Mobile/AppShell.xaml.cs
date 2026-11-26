using Ecommerce.Mobile.Services;
using Ecommerce.Mobile.Views;

namespace Ecommerce.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages pushed on top of the current tab.
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.ProductDetails, typeof(ProductDetailsPage));
        Routing.RegisterRoute(Routes.Checkout, typeof(CheckoutPage));
        Routing.RegisterRoute(Routes.OrderDetails, typeof(OrderDetailsPage));

#if DEBUG
        // Navigation trace (visible in logcat / IDE output) to help diagnose routing issues.
        Navigated += (_, e) => Console.WriteLine($"[NAV] {e.Previous?.Location} -> {e.Current?.Location} ({e.Source})");
#endif
    }
}
