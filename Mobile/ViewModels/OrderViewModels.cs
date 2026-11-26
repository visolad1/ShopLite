using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ecommerce.Mobile.Models;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.ViewModels;

public partial class OrdersViewModel(OrderService orders, IUiService ui) : BaseViewModel(ui)
{
    private bool _loadedOnce;

    public ObservableCollection<OrderSummary> Orders { get; } = [];

    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public override Task OnAppearingAsync() => LoadAsync(showBusy: !_loadedOnce);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(showBusy: false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        if (showBusy) IsEmpty = false;
        await RunAsync(async () =>
        {
            var list = await orders.GetOrdersAsync();
            Orders.Clear();
            foreach (var o in list) Orders.Add(o);
            IsEmpty = Orders.Count == 0;
            _loadedOnce = true;
        }, showBusy);
    }

    [RelayCommand]
    private Task OpenOrderAsync(OrderSummary order) =>
        Ui.GoToAsync(Routes.OrderDetails, new Dictionary<string, object> { ["id"] = order.Id });

    [RelayCommand]
    private Task StartShoppingAsync() => Ui.GoToAsync(Routes.Catalog);
}

public partial class OrderDetailsViewModel(OrderService orders, IUiService ui) : BaseViewModel(ui), IQueryAttributable
{
    private int _orderId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrder))]
    public partial Order? Order { get; set; }

    [ObservableProperty] public partial bool JustPlaced { get; set; }

    public bool HasOrder => Order is not null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && value is int id && id != _orderId)
        {
            _orderId = id;
            Order = null;
        }
        JustPlaced = query.TryGetValue("justPlaced", out var placed) && placed is true;
    }

    public override async Task OnAppearingAsync()
    {
        if (Order is null && _orderId > 0)
            await LoadAsync();
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        Order = await orders.GetOrderAsync(_orderId);
        Title = Order.Number;
    });
}
