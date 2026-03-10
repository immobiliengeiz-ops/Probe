using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AetherPlayer.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 120, 168));
    public Brush FalseBrush { get; set; } = new SolidColorBrush(Color.FromRgb(120, 130, 160));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
