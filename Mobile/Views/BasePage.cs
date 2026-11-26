using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

/// <summary>Base page: sets the view model and forwards the appearing lifecycle event to it.</summary>
public abstract class BasePage : ContentPage
{
    protected BasePage(BaseViewModel viewModel)
    {
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync(); // view models handle their own errors
    }
}
