using CommunityToolkit.Maui;
using Ecommerce.Mobile.Services;
using Ecommerce.Mobile.ViewModels;
using Ecommerce.Mobile.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace Ecommerce.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Configuration (embedded appsettings.json + appsettings.{Configuration}.json)
        builder.Services.AddSingleton(AppSettings.Load());

        // Services
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<SessionStore>());
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<OrderService>();
        builder.Services.AddSingleton<IUiService, UiService>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

        // Pages + view models
        AddPage<StartupPage, StartupViewModel>(builder.Services);
        AddPage<LoginPage, LoginViewModel>(builder.Services);
        AddPage<RegisterPage, RegisterViewModel>(builder.Services);
        AddPage<HomePage, HomeViewModel>(builder.Services);
        AddPage<CatalogPage, CatalogViewModel>(builder.Services);
        AddPage<ProductDetailsPage, ProductDetailsViewModel>(builder.Services);
        AddPage<CartPage, CartViewModel>(builder.Services);
        AddPage<CheckoutPage, CheckoutViewModel>(builder.Services);
        AddPage<OrdersPage, OrdersViewModel>(builder.Services);
        AddPage<OrderDetailsPage, OrderDetailsViewModel>(builder.Services);
        AddPage<ProfilePage, ProfileViewModel>(builder.Services);

        ConfigureHandlers();

        return builder.Build();
    }

    private static void AddPage<TPage, TViewModel>(IServiceCollection services)
        where TPage : Page where TViewModel : BaseViewModel
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();
    }

    /// <summary>Removes native underlines/borders so inputs blend into the rounded field style.</summary>
    private static void ConfigureHandlers()
    {
        EntryHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        EditorHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
    }
}
