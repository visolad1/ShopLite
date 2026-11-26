namespace Ecommerce.Mobile.Components;

public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty StatusProperty =
        BindableProperty.Create(nameof(Status), typeof(string), typeof(StatusBadge), string.Empty);

    public StatusBadge()
    {
        InitializeComponent();
        Root.BindingContext = this;
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
}
