using Ecommerce.Mobile.ViewModels;

namespace Ecommerce.Mobile.Views;

public partial class OrdersPage : BasePage
{
    public OrdersPage(OrdersViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
