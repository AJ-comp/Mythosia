using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts null values to Visibility.
    /// Null -> Collapsed, Non-null -> Visible (by default)
    /// Can be inverted using ConverterParameter = "Inverse"
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value != null;
            bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;

            if (isInverse)
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NullToVisibilityConverter does not support ConvertBack");
        }
    }
}