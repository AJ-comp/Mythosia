using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts enum values to Visibility based on matching with parameter.
    /// Usage: Visibility="{Binding Status, Converter={StaticResource EnumToVisibility}, ConverterParameter=Active}"
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EnumToVisibilityConverter does not support ConvertBack");
        }
    }
}