using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ecommerce.Mobile.Models;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.ViewModels;

public partial class CartViewModel(CartService cartService, IUiService ui) : BaseViewModel(ui)
{
    private bool _loadedOnce;

    public ObservableCollection<CartLine> Lines { get; } = [];

    [ObservableProperty] public partial decimal Total { get; set; }
    [ObservableProperty] public partial int TotalQuantity { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncreaseCommand), nameof(DecreaseCommand), nameof(RemoveCommand), nameof(CheckoutCommand))]
    public partial bool IsUpdating { get; set; }

    public bool HasItems => Lines.Count > 0;

    public override Task OnAppearingAsync() =>
        // Always refresh: prices/stock may have changed and items may have been added elsewhere.
        RunAsync(async () => Apply(await cartService.GetCartAsync()), showBusy: !_loadedOnce);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunAsync(async () => Apply(await cartService.GetCartAsync()), showBusy: false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task RetryAsync() => RunAsync(async () => Apply(await cartService.GetCartAsync()));

    [RelayCommand(CanExecute = nameof(CanModify))]
    private Task IncreaseAsync(CartLine line) => UpdateAsync(() => cartService.UpdateQuantityAsync(line.Id, line.Quantity + 1));

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task DecreaseAsync(CartLine line)
    {
        if (line.Quantity <= 1)
        {
            await RemoveAsync(line);
            return;
        }
        await UpdateAsync(() => cartService.UpdateQuantityAsync(line.Id, line.Quantity - 1));
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task RemoveAsync(CartLine line)
    {
        if (!await Ui.ConfirmAsync("Remove item", $"Remove \"{line.ProductName}\" from your cart?", "Remove"))
            return;
        await UpdateAsync(() => cartService.RemoveAsync(line.Id));
    }

    private bool CanModify(CartLine? line) => !IsUpdating;

    [RelayCommand(CanExecute = nameof(CanCheckout))]
    private Task CheckoutAsync() => Ui.GoToAsync(Routes.Checkout);

    private bool CanCheckout() => !IsUpdating && Lines.Count > 0;

    [RelayCommand]
    private Task StartShoppingAsync() => Ui.GoToAsync(Routes.Catalog);

    private async Task UpdateAsync(Func<Task<Cart>> call)
    {
        IsUpdating = true;
        try
        {
            Cart? cart = null;
            if (await RunActionAsync(async () => cart = await call(), "Couldn't update cart") && cart is not null)
                Apply(cart);
            else
                await RunAsync(async () => Apply(await cartService.GetCartAsync()), showBusy: false); // resync
        }
        finally
        {
            IsUpdating = false;
        }
    }

    /// <summary>Syncs the observable lines with the server cart in place (no list flicker).</summary>
    private void Apply(Cart cart)
    {
        var ids = cart.Items.Select(i => i.Id).ToHashSet();
        for (var i = Lines.Count - 1; i >= 0; i--)
            if (!ids.Contains(Lines[i].Id))
                Lines.RemoveAt(i);

        for (var index = 0; index < cart.Items.Count; index++)
        {
            var dto = cart.Items[index];
            var existing = Lines.FirstOrDefault(l => l.Id == dto.Id);
            if (existing is null)
                Lines.Insert(Math.Min(index, Lines.Count), new CartLine(dto));
            else
                existing.Update(dto);
        }

        Total = cart.Total;
        TotalQuantity = cart.TotalQuantity;
        IsEmpty = Lines.Count == 0;
        _loadedOnce = true;
        OnPropertyChanged(nameof(HasItems));
        CheckoutCommand.NotifyCanExecuteChanged();
    }
}

public partial class CheckoutViewModel(CartService cartService, OrderService orders, AuthService auth, IUiService ui)
    : BaseViewModel(ui)
{
    private static readonly Regex PhonePattern = new(@"^\+?[0-9\s\-()]{7,20}$", RegexOptions.Compiled);

    [ObservableProperty] public partial string CustomerName { get; set; } = string.Empty;
    [ObservableProperty] public partial string Phone { get; set; } = string.Empty;
    [ObservableProperty] public partial string DeliveryAddress { get; set; } = string.Empty;

    [ObservableProperty] public partial List<CartItemDto> Items { get; set; } = [];
    [ObservableProperty] public partial decimal Total { get; set; }
    [ObservableProperty] public partial int ItemCount { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlaceOrderCommand))]
    public partial bool IsPlacing { get; set; }

    [ObservableProperty] public partial string? ValidationMessage { get; set; }

    public override async Task OnAppearingAsync()
    {
        if (string.IsNullOrEmpty(CustomerName))
            CustomerName = auth.CurrentUser?.FullName ?? string.Empty;

        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var ok = await RunAsync(async () =>
        {
            var cart = await cartService.GetCartAsync();
            Items = cart.Items;
            Total = cart.Total;
            ItemCount = cart.TotalQuantity;
        });

        if (ok && Items.Count == 0)
        {
            await Ui.AlertAsync("Cart is empty", "Add some products before checking out.");
            await Ui.GoToAsync("..");
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlaceOrder))]
    private async Task PlaceOrderAsync()
    {
        ValidationMessage = Validate();
        if (ValidationMessage is not null) return;

        var confirmed = await Ui.ConfirmAsync(
            "Place order",
            $"Place your order for {Total.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}? You'll pay in cash on delivery.",
            "Place order");
        if (!confirmed) return;

        IsPlacing = true;
        try
        {
            Order? order = null;
            var ok = await RunActionAsync(async () =>
                order = await orders.PlaceOrderAsync(new CreateOrderRequest(CustomerName.Trim(), Phone.Trim(), DeliveryAddress.Trim())),
                "Couldn't place order");

            if (ok && order is not null)
            {
                Phone = DeliveryAddress = string.Empty;
                await Ui.GoToAsync(".."); // leave checkout
                await Ui.GoToAsync($"{Routes.Orders}/{Routes.OrderDetails}",
                    new Dictionary<string, object> { ["id"] = order.Id, ["justPlaced"] = true });
            }
            else
            {
                await LoadAsync(); // stock or prices may have changed - refresh the summary
            }
        }
        finally
        {
            IsPlacing = false;
        }
    }

    private bool CanPlaceOrder() => !IsPlacing;

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(CustomerName)) return "Please enter the recipient's name.";
        if (!PhonePattern.IsMatch(Phone.Trim())) return "Please enter a valid phone number.";
        if (DeliveryAddress.Trim().Length < 5) return "Please enter a full delivery address.";
        return null;
    }
}
