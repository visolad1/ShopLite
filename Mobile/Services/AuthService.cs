using System.Globalization;
using System.Text.Json;
using Ecommerce.Mobile.Models;

namespace Ecommerce.Mobile.Services;

/// <summary>
/// Holds the signed-in session. The JWT and profile are persisted with platform secure storage
/// (iOS Keychain / Android Keystore-backed encrypted preferences).
/// </summary>
public class SessionStore : ITokenProvider
{
    private const string TokenKey = "auth_token";
    private const string ExpiresKey = "auth_expires";
    private const string UserKey = "auth_user";

    public string? Token { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && ExpiresAt > DateTime.UtcNow && CurrentUser is not null;

    public async Task<bool> RestoreAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            var expires = await SecureStorage.Default.GetAsync(ExpiresKey);
            var user = await SecureStorage.Default.GetAsync(UserKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(user)
                || !DateTime.TryParse(expires, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp)
                || exp <= DateTime.UtcNow)
            {
                Clear();
                return false;
            }

            Token = token;
            ExpiresAt = exp.ToUniversalTime();
            CurrentUser = JsonSerializer.Deserialize<User>(user);
            return IsAuthenticated;
        }
        catch (Exception)
        {
            // Secure storage can become unreadable (e.g. after a device restore). Start clean.
            Clear();
            return false;
        }
    }

    public async Task SaveAsync(AuthResponse response)
    {
        Token = response.Token;
        ExpiresAt = response.ExpiresAt.ToUniversalTime();
        CurrentUser = response.User;

        await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        await SecureStorage.Default.SetAsync(ExpiresKey, ExpiresAt.Value.ToString("O", CultureInfo.InvariantCulture));
        await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(response.User));
    }

    public async Task UpdateUserAsync(User user)
    {
        CurrentUser = user;
        await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(user));
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        CurrentUser = null;
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(ExpiresKey);
        SecureStorage.Default.Remove(UserKey);
    }
}

public class AuthService(ApiClient api, SessionStore session)
{
    public User? CurrentUser => session.CurrentUser;
    public bool IsAuthenticated => session.IsAuthenticated;

    public Task<bool> RestoreSessionAsync() => session.RestoreAsync();

    public async Task LoginAsync(string email, string password)
    {
        var response = await api.PostAsync<AuthResponse>("api/auth/login", new LoginRequest(email.Trim(), password));
        await session.SaveAsync(response);
    }

    public async Task RegisterAsync(string fullName, string email, string password)
    {
        var response = await api.PostAsync<AuthResponse>("api/auth/register",
            new RegisterRequest(email.Trim(), password, fullName.Trim()));
        await session.SaveAsync(response);
    }

    /// <summary>Refreshes the cached profile from the server.</summary>
    public async Task<User> RefreshProfileAsync()
    {
        var user = await api.GetAsync<User>("api/auth/me");
        await session.UpdateUserAsync(user);
        return user;
    }

    /// <summary>JWTs are stateless, so logging out means discarding the stored token.</summary>
    public void Logout() => session.Clear();
}
