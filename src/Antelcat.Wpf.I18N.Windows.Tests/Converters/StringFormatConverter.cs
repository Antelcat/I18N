using System;
using System.Globalization;
using System.Windows.Data;

namespace Antelcat.Wpf.I18N.Windows.Tests.Converters;

public class StringFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Format(parameter as string ?? "{0}", value);
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}