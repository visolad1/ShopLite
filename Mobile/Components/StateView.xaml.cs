using System.Windows.Input;

namespace Ecommerce.Mobile.Components;

public partial class StateView : ContentView
{
    public static readonly BindableProperty IsLoadingProperty = Create(nameof(IsLoading), typeof(bool), false);
    public static readonly BindableProperty LoadingTextProperty = Create(nameof(LoadingText), typeof(string), "Loading...");
    public static readonly BindableProperty ErrorMessageProperty = Create(nameof(ErrorMessage), typeof(string), null);
    public static readonly BindableProperty RetryCommandProperty = Create(nameof(RetryCommand), typeof(ICommand), null);
    public static readonly BindableProperty IsEmptyProperty = Create(nameof(IsEmpty), typeof(bool), false);
    public static readonly BindableProperty EmptyIconProperty = Create(nameof(EmptyIcon), typeof(string), Ecommerce.Mobile.Resources.Styles.Icons.Inventory);
    public static readonly BindableProperty EmptyTitleProperty = Create(nameof(EmptyTitle), typeof(string), "Nothing here yet");
    public static readonly BindableProperty EmptyMessageProperty = Create(nameof(EmptyMessage), typeof(string), null);
    public static readonly BindableProperty ActionTextProperty = Create(nameof(ActionText), typeof(string), null);
    public static readonly BindableProperty ActionCommandProperty = Create(nameof(ActionCommand), typeof(ICommand), null);

    public StateView()
    {
        InitializeComponent();
        // Inner layout binds to this control; the control itself keeps the page's BindingContext.
        Root.BindingContext = this;
    }

    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public string? ErrorMessage { get => (string?)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }
    public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }
    public string EmptyIcon { get => (string)GetValue(EmptyIconProperty); set => SetValue(EmptyIconProperty, value); }
    public string EmptyTitle { get => (string)GetValue(EmptyTitleProperty); set => SetValue(EmptyTitleProperty, value); }
    public string? EmptyMessage { get => (string?)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    public bool ShowLoading => IsLoading;
    public bool ShowError => !IsLoading && !string.IsNullOrEmpty(ErrorMessage);
    public bool ShowEmpty => !IsLoading && string.IsNullOrEmpty(ErrorMessage) && IsEmpty;
    public bool HasAction => ActionCommand is not null && !string.IsNullOrEmpty(ActionText);

    private static BindableProperty Create(string name, Type type, object? defaultValue) =>
        BindableProperty.Create(name, type, typeof(StateView), defaultValue, propertyChanged: (b, _, _) => ((StateView)b).Refresh());

    private void Refresh()
    {
        OnPropertyChanged(nameof(ShowLoading));
        OnPropertyChanged(nameof(ShowError));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(HasAction));
    }
}
