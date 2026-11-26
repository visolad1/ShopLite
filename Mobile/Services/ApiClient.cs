using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ecommerce.Mobile.Services;

/// <summary>Error surfaced to view models with a user-friendly message.</summary>
public class ApiException(string message, HttpStatusCode? statusCode = null, bool isNetworkError = false, Exception? inner = null)
    : Exception(message, inner)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public bool IsNetworkError { get; } = isNetworkError;
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

/// <summary>Supplies the current bearer token (implemented by <see cref="AuthService"/>).</summary>
public interface ITokenProvider
{
    string? Token { get; }
}

/// <summary>
/// Single place for all HTTP communication: base URL, JSON, auth header, timeouts
/// and translation of network/HTTP failures into <see cref="ApiException"/>.
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ITokenProvider _tokens;

    /// <summary>Raised when an authenticated request is rejected with 401 (expired/invalid token).</summary>
    public event EventHandler? SessionExpired;

    public ApiClient(AppSettings settings, ITokenProvider tokens)
    {
        _tokens = tokens;
        _http = new HttpClient
        {
            BaseAddress = settings.Api.ResolveBaseUrl(),
            Timeout = TimeSpan.FromSeconds(settings.Api.TimeoutSeconds ?? 20)
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Uri BaseAddress => _http.BaseAddress!;

    public Task<T> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, ct);

    public Task<T> PatchAsync<T>(string path, object body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Patch, path, body, ct);

    public Task<T> DeleteAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.None)
            throw new ApiException("You appear to be offline. Check your internet connection and try again.", isNetworkError: true);

        using var request = new HttpRequestMessage(method, path.TrimStart('/'));
        var token = _tokens.Token;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or WebException)
        {
            var message = ex is TaskCanceledException
                ? "The server took too long to respond. Please try again."
                : "Can't reach the server. Check your connection and that the API is running.";
            throw new ApiException(message, isNetworkError: true, inner: ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = await response.Content.ReadFromJsonAsync<T>(Json, ct);
                    return result ?? throw new ApiException("The server returned an empty response.");
                }
                catch (JsonException ex)
                {
                    throw new ApiException("The server returned an unexpected response.", response.StatusCode, inner: ex);
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
            {
                SessionExpired?.Invoke(this, EventArgs.Empty);
                throw new ApiException("Your session has expired. Please sign in again.", response.StatusCode);
            }

            throw new ApiException(await ReadErrorMessageAsync(response, ct), response.StatusCode);
        }
    }

    /// <summary>Extracts a readable message from RFC 7807 problem / validation problem responses.</summary>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json, ct);
            if (problem?.Errors is { Count: > 0 })
                return string.Join("\n", problem.Errors.SelectMany(e => e.Value).Distinct());
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;
        }
        catch (Exception)
        {
            // Non-JSON error body; fall through to a generic message.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => "The requested item was not found.",
            HttpStatusCode.Unauthorized => "Invalid email or password.",
            >= HttpStatusCode.InternalServerError => "Something went wrong on our side. Please try again later.",
            _ => "The request could not be completed."
        };
    }

    private sealed class ProblemResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
