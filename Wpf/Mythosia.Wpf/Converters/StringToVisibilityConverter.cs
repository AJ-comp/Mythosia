using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts string values to Visibility.
    /// Non-empty string -> Visible, Empty/Null -> Collapsed
    /// Can be inverted using ConverterParameter = "Inverse"
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(value?.ToString());
            bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;

            if (isInverse)
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringToVisibilityConverter does not support ConvertBack");
        }
    }
}