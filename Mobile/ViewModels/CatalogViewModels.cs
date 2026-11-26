using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ecommerce.Mobile.Models;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.ViewModels;

public partial class HomeViewModel(ProductService products, AuthService auth, IUiService ui) : BaseViewModel(ui)
{
    private bool _loaded;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<Product> Featured { get; } = [];

    [ObservableProperty] public partial string Greeting { get; set; } = "Hello 👋";
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsRefreshing { get; set; }

    public bool IsEmpty => _loaded && Featured.Count == 0;

    public override async Task OnAppearingAsync()
    {
        var firstName = auth.CurrentUser?.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        Greeting = string.IsNullOrEmpty(firstName) ? "Hello 👋" : $"Hi, {firstName} 👋";

        if (!_loaded && !IsBusy)
            await LoadAsync(showBusy: true);
    }

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
        await RunAsync(async () =>
        {
            var categoriesTask = products.GetCategoriesAsync();
            var featuredTask = products.GetProductsAsync(featured: true);
            await Task.WhenAll(categoriesTask, featuredTask);

            Categories.Clear();
            foreach (var c in categoriesTask.Result) Categories.Add(c);
            Featured.Clear();
            foreach (var p in featuredTask.Result) Featured.Add(p);
            _loaded = true;
        }, showBusy);
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var text = SearchText.Trim();
        SearchText = string.Empty;
        await Ui.GoToAsync(Routes.Catalog, new Dictionary<string, object> { ["search"] = text, ["categoryId"] = 0 });
    }

    [RelayCommand]
    private Task OpenCategoryAsync(Category category) =>
        Ui.GoToAsync(Routes.Catalog, new Dictionary<string, object> { ["categoryId"] = category.Id, ["search"] = string.Empty });

    [RelayCommand]
    private Task SeeAllAsync() =>
        Ui.GoToAsync(Routes.Catalog, new Dictionary<string, object> { ["categoryId"] = 0, ["search"] = string.Empty });

    [RelayCommand]
    private Task OpenProductAsync(Product product) =>
        Ui.GoToAsync(Routes.ProductDetails, new Dictionary<string, object> { ["id"] = product.Id });
}

public partial class CatalogViewModel(ProductService products, IUiService ui) : BaseViewModel(ui), IQueryAttributable
{
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _debounceCts;
    private bool _categoriesLoaded;
    private bool _needsReload = true;
    private bool _suppressSearchReload;

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<CategoryChip> Categories { get; } = [];

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial int? SelectedCategoryId { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial string ResultsText { get; set; } = string.Empty;

    /// <summary>Receives filters from the Home screen (search text / category).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("search", out var s))
        {
            _suppressSearchReload = true;
            SearchText = s as string ?? string.Empty;
            _suppressSearchReload = false;
        }

        if (query.TryGetValue("categoryId", out var c) && c is int id)
            SelectedCategoryId = id > 0 ? id : null;

        _needsReload = true;
        query.Clear(); // don't re-apply when navigating back to this tab
    }

    public override async Task OnAppearingAsync()
    {
        if (!_categoriesLoaded)
            await LoadCategoriesAsync();

        if (_needsReload)
        {
            _needsReload = false;
            await LoadProductsAsync(showBusy: true);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressSearchReload) return;

        // Debounce typing so we don't call the API on every keystroke.
        _debounceCts?.Cancel();
        var cts = _debounceCts = new CancellationTokenSource();
        _ = Task.Delay(400, cts.Token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
                await MainThread.InvokeOnMainThreadAsync(() => LoadProductsAsync(showBusy: true));
        }, TaskScheduler.Default);
    }

    partial void OnSelectedCategoryIdChanged(int? value)
    {
        foreach (var chip in Categories)
            chip.IsSelected = chip.Id == value;
    }

    [RelayCommand]
    private Task SearchAsync()
    {
        _debounceCts?.Cancel();
        return LoadProductsAsync(showBusy: true);
    }

    [RelayCommand]
    private Task SelectCategoryAsync(CategoryChip chip)
    {
        if (SelectedCategoryId == chip.Id) return Task.CompletedTask;
        SelectedCategoryId = chip.Id;
        return LoadProductsAsync(showBusy: true);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!_categoriesLoaded) await LoadCategoriesAsync();
        await LoadProductsAsync(showBusy: false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        if (!_categoriesLoaded) await LoadCategoriesAsync();
        await LoadProductsAsync(showBusy: true);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _suppressSearchReload = true;
        SearchText = string.Empty;
        _suppressSearchReload = false;
        SelectedCategoryId = null;
        _ = LoadProductsAsync(showBusy: true);
    }

    [RelayCommand]
    private Task OpenProductAsync(Product product) =>
        Ui.GoToAsync(Routes.ProductDetails, new Dictionary<string, object> { ["id"] = product.Id });

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await products.GetCategoriesAsync();
            Categories.Clear();
            Categories.Add(new CategoryChip(null, "All", "✨"));
            foreach (var c in categories)
                Categories.Add(new CategoryChip(c.Id, c.Name, c.Icon));
            OnSelectedCategoryIdChanged(SelectedCategoryId);
            _categoriesLoaded = true;
        }
        catch (Exception)
        {
            // Product loading reports the error state; chips are retried on refresh.
        }
    }

    private async Task LoadProductsAsync(bool showBusy)
    {
        // Cancel any in-flight request so stale results never overwrite newer ones.
        _loadCts?.Cancel();
        var cts = _loadCts = new CancellationTokenSource();

        if (showBusy)
        {
            Products.Clear();
            IsEmpty = false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await products.GetProductsAsync(SearchText, SelectedCategoryId, ct: cts.Token);
            if (cts.IsCancellationRequested) return;

            Products.Clear();
            foreach (var p in result) Products.Add(p);
            IsEmpty = Products.Count == 0;
            ResultsText = Products.Count == 1 ? "1 product" : $"{Products.Count} products";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Superseded by a newer request.
        }
        catch (Exception ex)
        {
            if (cts.IsCancellationRequested) return;
            Products.Clear();
            ErrorMessage = ToMessage(ex);
            ResultsText = string.Empty;
        }
        finally
        {
            if (_loadCts == cts) IsBusy = false;
        }
    }
}

public partial class ProductDetailsViewModel(ProductService products, CartService cart, IUiService ui)
    : BaseViewModel(ui), IQueryAttributable
{
    private int _productId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProduct), nameof(AvailabilityText), nameof(IsInStock), nameof(IsLowStock), nameof(MaxQuantity))]
    [NotifyCanExecuteChangedFor(nameof(AddToCartCommand), nameof(IncreaseCommand))]
    public partial Product? Product { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncreaseCommand), nameof(DecreaseCommand))]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToCartCommand))]
    public partial bool IsAdding { get; set; }

    public bool HasProduct => Product is not null;
    public bool IsInStock => Product?.InStock == true;
    public bool IsLowStock => Product is { InStock: true, Stock: <= 5 };
    public int MaxQuantity => Math.Min(Product?.Stock ?? 0, 99);

    public string AvailabilityText => Product switch
    {
        null => string.Empty,
        { InStock: false } => "Out of stock",
        { Stock: <= 5 } p => $"Only {p.Stock} left in stock",
        _ => "In stock"
    };

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && value is int id && id != _productId)
        {
            _productId = id;
            Product = null;
            Quantity = 1;
        }
    }

    public override async Task OnAppearingAsync()
    {
        if (Product is null && _productId > 0)
            await LoadAsync();
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        Product = await products.GetProductAsync(_productId);
        Title = Product.Name;
        Quantity = Math.Clamp(Quantity, 1, Math.Max(1, MaxQuantity));
    });

    [RelayCommand(CanExecute = nameof(CanIncrease))]
    private void Increase() => Quantity++;

    private bool CanIncrease() => Quantity < MaxQuantity;

    [RelayCommand(CanExecute = nameof(CanDecrease))]
    private void Decrease() => Quantity--;

    private bool CanDecrease() => Quantity > 1;

    [RelayCommand(CanExecute = nameof(CanAddToCart))]
    private async Task AddToCartAsync()
    {
        if (Product is null) return;
        IsAdding = true;
        try
        {
            var added = await RunActionAsync(() => cart.AddAsync(Product.Id, Quantity), "Couldn't add to cart");
            if (added)
            {
                await Ui.ToastAsync($"Added {Quantity} × {Product.Name} to your cart");
                Quantity = 1;
            }
        }
        finally
        {
            IsAdding = false;
        }
    }

    private bool CanAddToCart() => Product?.InStock == true && !IsAdding;

    [RelayCommand]
    private Task GoToCartAsync() => Ui.GoToAsync(Routes.Cart);
}
