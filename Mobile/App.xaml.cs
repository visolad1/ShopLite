using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly AuthService _auth;
    private readonly IUiService _ui;
    private int _handlingExpiry;

    public App(IServiceProvider services, ApiClient api, AuthService auth, IUiService ui)
    {
        InitializeComponent();
        _services = services;
        _auth = auth;
        _ui = ui;

        // Central handling of 401 responses: clear the session and return to login once.
        api.SessionExpired += OnSessionExpired;
    }

    // Resolve the Shell only after InitializeComponent has loaded the app-wide resources it uses.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_services.GetRequiredService<AppShell>());

    private async void OnSessionExpired(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _handlingExpiry, 1) == 1) return;
        try
        {
            _auth.Logout();
            await _ui.GoToAsync(Routes.Login);
            await _ui.AlertAsync("Session expired", "Please sign in again to continue.");
        }
        finally
        {
            Interlocked.Exchange(ref _handlingExpiry, 0);
        }
    }
}
