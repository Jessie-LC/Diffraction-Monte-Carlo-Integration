using System;
using System.Globalization;
using System.Windows.Data;

namespace Diffraction_Monte_Carlo_Integration.UI.Converters;

internal class NullableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return null;

        return value;
    }
}
