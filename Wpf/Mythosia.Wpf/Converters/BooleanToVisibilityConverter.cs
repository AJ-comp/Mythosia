using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts boolean values to Visibility values.
    /// True -> Visible, False -> Collapsed (by default)
    /// Can be inverted using ConverterParameter = "Inverse"
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;

                if (isInverse)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;

                if (isInverse)
                {
                    return visibility != Visibility.Visible;
                }

                return visibility == Visibility.Visible;
            }

            return false;
        }
    }
}