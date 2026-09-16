using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ServiceLib.Common;

namespace v2rayN.Converters;

public sealed class CountryFlagConverter : IValueConverter
{
    private static readonly Dictionary<string, BitmapImage> Cache = new();
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var code = ProfileCountry.Normalize(value as string);
        if (code == null) return null;
        if (Cache.TryGetValue(code, out var cached)) return cached;
        using var stream = ProfileCountry.OpenFlag(code);
        if (stream == null) return null;
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        Cache[code] = image;
        return image;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
