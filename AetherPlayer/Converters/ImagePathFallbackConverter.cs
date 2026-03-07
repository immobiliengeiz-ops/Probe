using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace AetherPlayer.Converters;

public class ImagePathFallbackConverter : IValueConverter
{
    public string FallbackPath { get; set; } = "Assets/Covers/cover1.png";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = value?.ToString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return FallbackPath;
        }

        var fullPath = Path.GetFullPath(path);
        return File.Exists(fullPath) ? path : FallbackPath;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
