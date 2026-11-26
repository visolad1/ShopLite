using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

public partial class ProductDetailsPage : BasePage
{
    public ProductDetailsPage(ProductDetailsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
