using System.Text.RegularExpressions;

namespace Ecommerce.Mobile.Services;

/// <summary>
/// Requests appropriately sized images from the image CDN so lists don't download and decode
/// full-size photos while scrolling. Unknown hosts are returned unchanged.
/// </summary>
public static partial class ImageUrls
{
    public static string Resize(string? url, int size)
    {
        if (string.IsNullOrEmpty(url) || !url.Contains("images.unsplash.com", StringComparison.OrdinalIgnoreCase))
            return url ?? string.Empty;

        var result = WidthParam().Replace(url, $"w={size}");
        return HeightParam().Replace(result, $"h={size}");
    }

    [GeneratedRegex(@"(?<=[?&])w=\d+")]
    private static partial Regex WidthParam();

    [GeneratedRegex(@"(?<=[?&])h=\d+")]
    private static partial Regex HeightParam();
}
