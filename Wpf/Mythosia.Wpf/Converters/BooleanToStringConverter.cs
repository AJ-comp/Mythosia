using System;
using System.Globalization;
using System.Windows.Data;

namespace Mythosia.Wpf.Converters
{
    /// <summary>
    /// Converts boolean values to string values.
    /// True -> "True", False -> "False" (by default)
    /// Custom strings can be provided using ConverterParameter format: "TrueString|FalseString"
    /// Usage: Text="{Binding IsActive, Converter={StaticResource BooleanToString}, ConverterParameter='Yes|No'}"
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        /// <summary>
        /// String to display when value is true (default: "True")
        /// </summary>
        public string TrueString { get; set; } = "True";

        /// <summary>
        /// String to display when value is false (default: "False")
        /// </summary>
        public string FalseString { get; set; } = "False";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                string trueText = TrueString;
                string falseText = FalseString;

                // Parse parameter if provided in format "TrueString|FalseString"
                if (parameter is string paramString && !string.IsNullOrEmpty(paramString))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
                        trueText = parts[0];
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                        falseText = parts[1];
                }

                return boolValue ? trueText : falseText;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                string trueText = TrueString;
                string falseText = FalseString;

                // Parse parameter if provided in format "TrueString|FalseString"
                if (parameter is string paramString && !string.IsNullOrEmpty(paramString))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
                        trueText = parts[0];
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                        falseText = parts[1];
                }

                if (stringValue.Equals(trueText, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (stringValue.Equals(falseText, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Try parsing as boolean
                if (bool.TryParse(stringValue, out bool result))
                    return result;
            }

            return false;
        }
    }
}