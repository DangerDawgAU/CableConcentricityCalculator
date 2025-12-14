using Avalonia.Media;

namespace CableConcentricityCalculator.Gui.Utilities;

/// <summary>
/// Shared color utilities for Avalonia GUI
/// </summary>
public static class ColorUtilities
{
    /// <summary>
    /// Standard color mapping for cable jacket colors (Avalonia format)
    /// </summary>
    public static readonly Dictionary<string, Color> ColorMap = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>
    /// Get an Avalonia color by name, with fallback to gray if not found
    /// </summary>
    public static Color GetColor(string colorName, Color? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return fallback ?? Color.Parse("#C8C8C8");

        return ColorMap.TryGetValue(colorName, out var color)
            ? color
            : fallback ?? Color.Parse("#C8C8C8");
    }
}
