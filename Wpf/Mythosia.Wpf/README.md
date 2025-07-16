# Mythosia.Wpf
This project provides a collection of useful WPF value converters that simplify data binding in XAML. These converters handle common scenarios in WPF applications and follow consistent naming and usage patterns.

## Features
- **Comprehensive Converter Collection**: A complete set of value converters for common WPF binding scenarios
- **Consistent API**: All converters follow the same design patterns and conventions
- **Bidirectional Support**: Most converters support both Convert and ConvertBack operations where applicable
- **Parameter Support**: Many converters support additional customization through ConverterParameter
- **Inversion Support**: Several converters support inverse behavior through parameters

## Available Converters

### Boolean Converters
- **`BooleanToVisibilityConverter`**: Converts boolean values to Visibility
- **`InverseBooleanConverter`**: Inverts boolean values (True ¡ê False)
- **`InverseBooleanToVisibilityConverter`**: Converts boolean to Visibility with inversion
- **`BooleanToStringConverter`**: Converts boolean values to custom strings

### String Converters
- **`StringToVisibilityConverter`**: Converts string values to Visibility based on null/empty state

### Object Converters
- **`NullToVisibilityConverter`**: Converts null values to Visibility

### Enum Converters
- **`EnumToVisibilityConverter`**: Converts enum values to Visibility based on parameter matching
- **`EnumToBooleanConverter`**: Converts enum values to boolean for RadioButton binding

## Installation
Add a reference to the Mythosia.Wpf project or include the converter classes in your WPF application.

## Usage

### 1. Add Namespace Reference
Add the namespace reference to your XAML file:

```xml
xmlns:converters="clr-namespace:Mythosia.Wpf.Converters"
```

### 2. Define Converters as Resources
Define the converters in your application or window resources:

```xml
<Window.Resources>
    <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibility" />
    <converters:StringToVisibilityConverter x:Key="StringToVisibility" />
    <converters:NullToVisibilityConverter x:Key="NullToVisibility" />
    <converters:BooleanToStringConverter x:Key="BooleanToString" />
    <converters:EnumToVisibilityConverter x:Key="EnumToVisibility" />
    <converters:EnumToBooleanConverter x:Key="EnumToBoolean" />
    <converters:InverseBooleanConverter x:Key="InverseBoolean" />
    <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibility" />
</Window.Resources>
```

### 3. Use Converters in Bindings

#### BooleanToVisibilityConverter
```xml
<!-- Show element when boolean is true -->
<TextBlock Text="Visible when true" 
           Visibility="{Binding IsVisible, Converter={StaticResource BooleanToVisibility}}" />

<!-- Inverse behavior -->
<TextBlock Text="Hidden when true" 
           Visibility="{Binding IsVisible, Converter={StaticResource BooleanToVisibility}, ConverterParameter=Inverse}" />
```

#### StringToVisibilityConverter
```xml
<!-- Show element when string is not empty -->
<TextBlock Text="Has content" 
           Visibility="{Binding Message, Converter={StaticResource StringToVisibility}}" />

<!-- Show element when string is empty (inverse) -->
<TextBlock Text="No content" 
           Visibility="{Binding Message, Converter={StaticResource StringToVisibility}, ConverterParameter=Inverse}" />
```

#### NullToVisibilityConverter
```xml
<!-- Show element when object is not null -->
<Button Content="Edit" 
        Visibility="{Binding SelectedItem, Converter={StaticResource NullToVisibility}}" />

<!-- Show element when object is null (inverse) -->
<TextBlock Text="No selection" 
           Visibility="{Binding SelectedItem, Converter={StaticResource NullToVisibility}, ConverterParameter=Inverse}" />
```

#### BooleanToStringConverter
```xml
<!-- Basic usage with default strings (True/False) -->
<TextBlock Text="{Binding IsActive, Converter={StaticResource BooleanToString}}" />

<!-- Custom strings using parameter -->
<TextBlock Text="{Binding IsOnline, Converter={StaticResource BooleanToString}, ConverterParameter='Online|Offline'}" />

<!-- Using properties for custom strings -->
<converters:BooleanToStringConverter x:Key="YesNoConverter" TrueString="Yes" FalseString="No" />
<TextBlock Text="{Binding IsConfirmed, Converter={StaticResource YesNoConverter}}" />
```

#### EnumToVisibilityConverter
```xml
<!-- Show element when enum matches parameter -->
<TextBlock Text="Grid View Active" 
           Visibility="{Binding ViewMode, Converter={StaticResource EnumToVisibility}, ConverterParameter=Grid}" />
```

#### EnumToBooleanConverter
```xml
<!-- RadioButton binding -->
<RadioButton Content="Grid View" 
             IsChecked="{Binding ViewMode, Converter={StaticResource EnumToBoolean}, ConverterParameter=Grid}" />
<RadioButton Content="List View" 
             IsChecked="{Binding ViewMode, Converter={StaticResource EnumToBoolean}, ConverterParameter=List}" />
```

#### InverseBooleanConverter
```xml
<!-- Invert boolean value -->
<CheckBox Content="Disabled" 
          IsChecked="{Binding IsEnabled, Converter={StaticResource InverseBoolean}}" />
```

#### InverseBooleanToVisibilityConverter
```xml
<!-- Hide element when boolean is true -->
<TextBlock Text="Hidden when true" 
           Visibility="{Binding IsLoading, Converter={StaticResource InverseBooleanToVisibility}}" />
```

## Converter Details

### BooleanToVisibilityConverter
- **Purpose**: Converts boolean values to Visibility
- **Behavior**: True ¡æ Visible, False ¡æ Collapsed
- **Inversion**: Supports "Inverse" parameter
- **ConvertBack**: Supported

### StringToVisibilityConverter
- **Purpose**: Shows/hides elements based on string content
- **Behavior**: Non-empty ¡æ Visible, Empty/Null ¡æ Collapsed
- **Inversion**: Supports "Inverse" parameter
- **ConvertBack**: Not supported

### NullToVisibilityConverter
- **Purpose**: Shows/hides elements based on null state
- **Behavior**: Non-null ¡æ Visible, Null ¡æ Collapsed
- **Inversion**: Supports "Inverse" parameter
- **ConvertBack**: Not supported

### BooleanToStringConverter
- **Purpose**: Converts boolean values to custom text
- **Behavior**: Configurable through properties or parameter
- **Parameter Format**: "TrueString|FalseString"
- **Properties**: TrueString, FalseString
- **ConvertBack**: Supported

### EnumToVisibilityConverter
- **Purpose**: Shows elements based on enum value matching
- **Parameter**: Target enum value as string
- **ConvertBack**: Not supported

### EnumToBooleanConverter
- **Purpose**: RadioButton binding with enums
- **Parameter**: Target enum value as string
- **ConvertBack**: Supported

### InverseBooleanConverter
- **Purpose**: Simple boolean inversion
- **ConvertBack**: Supported

### InverseBooleanToVisibilityConverter
- **Purpose**: Boolean to Visibility with built-in inversion
- **Properties**: HiddenVisibility (Collapsed/Hidden)
- **ConvertBack**: Supported

## Design Principles
- **Consistency**: All converters follow the same naming and parameter conventions
- **Flexibility**: Support for both properties and parameters for customization
- **Reliability**: Proper null handling and fallback values
- **Performance**: Efficient implementations with minimal overhead

## Requirements
- .NET 8
- WPF Framework

## License
MIT License - see the main Mythosia project for details.

## Contributing
This project is part of the larger Mythosia ecosystem. Please refer to the main repository for contribution guidelines.