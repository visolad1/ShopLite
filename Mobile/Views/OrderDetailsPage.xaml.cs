using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

public partial class OrderDetailsPage : BasePage
{
    public OrderDetailsPage(OrderDetailsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
