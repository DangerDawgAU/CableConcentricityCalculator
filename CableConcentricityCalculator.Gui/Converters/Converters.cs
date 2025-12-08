using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CableConcentricityCalculator.Gui.Converters;

public class BoolToModifiedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "Modified" : "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i > 0,
            double d => d > 0,
            float f => f > 0,
            _ => false
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i == 0,
            double d => d == 0,
            float f => f == 0,
            _ => true
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ColorNameConverter : IValueConverter
{
    private static readonly Dictionary<string, Color> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "White", Colors.White },
        { "Black", Colors.Black },
        { "Red", Color.Parse("#DC1414") },
        { "Green", Color.Parse("#009600") },
        { "Blue", Color.Parse("#0050B4") },
        { "Yellow", Color.Parse("#F0DC00") },
        { "Orange", Color.Parse("#FF8C00") },
        { "Brown", Color.Parse("#8B5A2B") },
        { "Violet", Color.Parse("#9400D3") },
        { "Purple", Color.Parse("#9400D3") },
        { "Gray", Color.Parse("#808080") },
        { "Grey", Color.Parse("#808080") },
        { "Pink", Color.Parse("#FFB6C1") },
        { "Natural", Color.Parse("#F5F0DC") },
        { "Clear", Color.Parse("#DCDCDC") },
        { "Silver", Color.Parse("#C0C0C0") },
        { "Tan", Color.Parse("#D2B48C") },
        { "Nylon", Color.Parse("#F5F5DC") }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorName && ColorMap.TryGetValue(colorName, out var color))
        {
            return color;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumValuesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        return Enum.GetValues(value.GetType());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
