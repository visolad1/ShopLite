using System.Reflection;
using System.Text.Json;

namespace Ecommerce.Mobile.Services;

public class ApiSettings
{
    /// <summary>Default API base URL (iOS simulator / desktop: localhost).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Optional Android override. The Android emulator reaches the host machine via 10.0.2.2.</summary>
    public string? AndroidBaseUrl { get; set; }

    public int? TimeoutSeconds { get; set; }

    /// <summary>Base URL that applies to the current platform.</summary>
    public Uri ResolveBaseUrl()
    {
        var url = DeviceInfo.Platform == DevicePlatform.Android && !string.IsNullOrWhiteSpace(AndroidBaseUrl)
            ? AndroidBaseUrl
            : BaseUrl;

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Api:BaseUrl is not configured in appsettings.json.");

        return new Uri(url.EndsWith('/') ? url : url + "/");
    }
}

public class AppSettings
{
    public ApiSettings Api { get; set; } = new();

    /// <summary>
    /// Loads the embedded appsettings.json and overlays appsettings.{Configuration}.json when it was embedded.
    /// </summary>
    public static AppSettings Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var settings = Read(assembly, "appsettings.json") ?? new AppSettings();
        var overlay = Read(assembly, "appsettings.environment.json");

        if (overlay is not null)
        {
            settings.Api.BaseUrl = overlay.Api.BaseUrl ?? settings.Api.BaseUrl;
            settings.Api.AndroidBaseUrl = overlay.Api.AndroidBaseUrl ?? settings.Api.AndroidBaseUrl;
            settings.Api.TimeoutSeconds = overlay.Api.TimeoutSeconds ?? settings.Api.TimeoutSeconds;
        }

        return settings;
    }

    private static AppSettings? Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        return stream is null
            ? null
            : JsonSerializer.Deserialize<AppSettings>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
