using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AetherPlayer.Converters;

public class ImagePathFallbackConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public string FallbackPath { get; set; } = "Assets/Covers/cover1.png";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var inputPath = value?.ToString();
        var fallbackCandidate = !string.IsNullOrWhiteSpace(parameter?.ToString()) ? parameter!.ToString()! : FallbackPath;

        var resolved = ResolvePath(inputPath) ?? ResolvePath(fallbackCandidate);
        if (resolved is not null)
        {
            return Cache.GetOrAdd(resolved, CreateBitmapImage);
        }

        return CreatePlaceholderImage();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static string? ResolvePath(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri))
        {
            if (!absoluteUri.IsFile)
            {
                return null;
            }

            return File.Exists(absoluteUri.LocalPath) ? absoluteUri.LocalPath : null;
        }

        var baseDir = AppContext.BaseDirectory;
        var rootedFromBase = Path.GetFullPath(Path.Combine(baseDir, candidate));
        if (File.Exists(rootedFromBase))
        {
            return rootedFromBase;
        }

        var rootedFromCwd = Path.GetFullPath(candidate);
        if (File.Exists(rootedFromCwd))
        {
            return rootedFromCwd;
        }

        return null;
    }

    private static ImageSource CreateBitmapImage(string absolutePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = new Uri(absolutePath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static ImageSource CreatePlaceholderImage()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(32, 38, 56)), null, new RectangleGeometry(new Rect(0, 0, 100, 100))));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(124, 140, 255)), null, new RectangleGeometry(new Rect(0, 72, 100, 28))));
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
