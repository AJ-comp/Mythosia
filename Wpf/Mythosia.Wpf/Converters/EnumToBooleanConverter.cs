using System;
using System.Globalization;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts enum values to boolean for RadioButton binding.
    /// Usage: IsChecked="{Binding ViewMode, Converter={StaticResource EnumToBoolean}, ConverterParameter=Grid}"
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                // Parse the enum value from parameter
                return Enum.Parse(targetType, parameter.ToString());
            }

            return Binding.DoNothing;
        }
    }
}