using System.Globalization;
using System.Windows.Data;

namespace AetherPlayer.Converters;

public class EnumMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) == true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
