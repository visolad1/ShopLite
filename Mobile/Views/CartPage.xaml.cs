using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

public partial class CartPage : BasePage
{
    public CartPage(CartViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
