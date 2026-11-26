using System.Globalization;

namespace Ecommerce.Mobile.Converters;

/// <summary>Formats prices in US dollars independent of device culture (catalog prices are USD).</summary>
public class CurrencyConverter : IValueConverter
{
    private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            decimal d => d.ToString("C2", Usd),
            double d => d.ToString("C2", Usd),
            int i => i.ToString("C2", Usd),
            _ => string.Empty
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps an order status to a badge color. ConverterParameter "bg" returns a translucent background,
/// otherwise the solid foreground color.
/// </summary>
public class OrderStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = (value as string) switch
        {
            "Pending" => Color.FromArgb("#D97706"),
            "Confirmed" => Color.FromArgb("#4F46E5"),
            "Shipped" => Color.FromArgb("#0284C7"),
            "Delivered" => Color.FromArgb("#16A34A"),
            "Cancelled" => Color.FromArgb("#DC2626"),
            _ => Color.FromArgb("#6B7280")
        };
        return parameter as string == "bg" ? color.WithAlpha(0.14f) : color;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
