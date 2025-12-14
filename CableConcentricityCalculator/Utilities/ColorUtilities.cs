using SkiaSharp;

namespace CableConcentricityCalculator.Utilities;

/// <summary>
/// Shared utilities for cable calculations and visualization
/// </summary>
public static class CableUtilities
{
    /// <summary>
    /// Random circular packing efficiency (~63.7%)
    /// Based on empirical studies of random close packing of circles.
    /// Note: Optimal hexagonal packing achieves ~90.69%, but random packing is more realistic for cable bundles.
    /// </summary>
    public const double PackingEfficiency = 0.637;

    /// <summary>
    /// Calculate the cross-sectional area of a circle given its diameter
    /// </summary>
    /// <param name="diameter">Diameter in mm</param>
    /// <returns>Area in mm²</returns>
    public static double GetCircularArea(double diameter)
    {
        var radius = diameter / 2;
        return Math.PI * radius * radius;
    }
}

/// <summary>
/// Shared color utilities for cable visualization
/// </summary>
public static class ColorUtilities
{
    /// <summary>
    /// Standard color mapping for cable jacket colors (SkiaSharp format)
    /// </summary>
    public static readonly Dictionary<string, SKColor> ColorMapSK = new(StringComparer.OrdinalIgnoreCase)
    {
        { "White", SKColors.White },
        { "Black", SKColors.Black },
        { "Red", new SKColor(220, 20, 20) },
        { "Green", new SKColor(0, 150, 0) },
        { "Blue", new SKColor(0, 80, 180) },
        { "Yellow", new SKColor(240, 220, 0) },
        { "Orange", new SKColor(255, 140, 0) },
        { "Brown", new SKColor(139, 90, 43) },
        { "Violet", new SKColor(148, 0, 211) },
        { "Purple", new SKColor(148, 0, 211) },
        { "Gray", new SKColor(128, 128, 128) },
        { "Grey", new SKColor(128, 128, 128) },
        { "Pink", new SKColor(255, 182, 193) },
        { "Natural", new SKColor(245, 240, 220) },
        { "Clear", new SKColor(220, 220, 220) },
        { "Silver", new SKColor(192, 192, 192) },
        { "Tan", new SKColor(210, 180, 140) },
        { "Nylon", new SKColor(245, 245, 220) }
    };

    /// <summary>
    /// Get a SkiaSharp color by name, with fallback to gray if not found
    /// </summary>
    public static SKColor GetColorSK(string colorName, SKColor? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return fallback ?? new SKColor(200, 200, 200);

        return ColorMapSK.TryGetValue(colorName, out var color)
            ? color
            : fallback ?? new SKColor(200, 200, 200);
    }
}
