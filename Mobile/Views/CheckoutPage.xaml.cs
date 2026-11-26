using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

public partial class CheckoutPage : BasePage
{
    public CheckoutPage(CheckoutViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
