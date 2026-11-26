using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.ViewModels;

/// <summary>Decides the first screen: restores a saved session or shows the login page.</summary>
public class StartupViewModel(AuthService auth, IUiService ui) : BaseViewModel(ui)
{
    private bool _started;

    public override async Task OnAppearingAsync()
    {
        if (_started) return;
        _started = true;

        var restored = await auth.RestoreSessionAsync();
        await Ui.GoToAsync(restored ? Routes.Home : Routes.Login);
    }
}

public partial class LoginViewModel(AuthService auth, IUiService ui) : BaseViewModel(ui)
{
    [ObservableProperty] public partial string Email { get; set; } = string.Empty;
    [ObservableProperty] public partial string Password { get; set; } = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            return;
        }

        if (await RunAsync(() => auth.LoginAsync(Email, Password)))
        {
            Password = string.Empty;
            await Ui.GoToAsync(Routes.Home);
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        ErrorMessage = null;
        await Ui.GoToAsync(Routes.Register);
    }
}

public partial class RegisterViewModel(AuthService auth, IUiService ui) : BaseViewModel(ui)
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [ObservableProperty] public partial string FullName { get; set; } = string.Empty;
    [ObservableProperty] public partial string Email { get; set; } = string.Empty;
    [ObservableProperty] public partial string Password { get; set; } = string.Empty;
    [ObservableProperty] public partial string ConfirmPassword { get; set; } = string.Empty;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        ErrorMessage = Validate();
        if (ErrorMessage is not null) return;

        if (await RunAsync(() => auth.RegisterAsync(FullName, Email, Password)))
        {
            Password = ConfirmPassword = string.Empty;
            await Ui.GoToAsync(Routes.Home);
        }
    }

    [RelayCommand]
    private Task GoToLoginAsync() => Ui.GoToAsync("..");

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(FullName)) return "Please enter your name.";
        if (!EmailPattern.IsMatch(Email.Trim())) return "Please enter a valid email address.";
        if (Password.Length < 8) return "Password must be at least 8 characters long.";
        if (Password != ConfirmPassword) return "Passwords do not match.";
        return null;
    }
}

public partial class ProfileViewModel(AuthService auth, ApiClient api, IUiService ui) : BaseViewModel(ui)
{
    [ObservableProperty] public partial string FullName { get; set; } = string.Empty;
    [ObservableProperty] public partial string Email { get; set; } = string.Empty;
    [ObservableProperty] public partial string Initials { get; set; } = string.Empty;
    [ObservableProperty] public partial string MemberSince { get; set; } = string.Empty;

    public string ServerInfo => $"Server: {api.BaseAddress}";
    public string AppVersion => $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    public override async Task OnAppearingAsync()
    {
        Apply();
        try
        {
            await auth.RefreshProfileAsync();
            Apply();
        }
        catch (Exception)
        {
            // Cached profile is good enough when offline; 401 is handled globally.
        }
    }

    private void Apply()
    {
        var user = auth.CurrentUser;
        if (user is null) return;

        FullName = user.FullName;
        Email = user.Email;
        MemberSince = $"Member since {user.CreatedAt.ToLocalTime():MMMM yyyy}";
        Initials = string.Concat(user.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => char.ToUpperInvariant(p[0])));
    }

    [RelayCommand]
    private Task ViewOrdersAsync() => Ui.GoToAsync(Routes.Orders);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await Ui.ConfirmAsync("Log out", "Are you sure you want to log out?", "Log out"))
            return;

        auth.Logout();
        await Ui.GoToAsync(Routes.Login);
    }
}
