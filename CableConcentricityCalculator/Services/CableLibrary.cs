using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Legacy cable library wrapper - now loads from user library
/// Kept for backward compatibility with existing code
/// </summary>
public static class CableLibrary
{
    /// <summary>
    /// Standard AWG wire gauges with conductor diameters and typical insulation thickness
    /// Reference data for cable creation
    /// </summary>
    public static readonly Dictionary<string, (double ConductorDia, double InsulationThick)> AwgSizes = new()
    {
        { "30", (0.254, 0.10) },
        { "28", (0.320, 0.10) },
        { "26", (0.405, 0.15) },
        { "24", (0.511, 0.15) },
        { "22", (0.644, 0.20) },
        { "20", (0.812, 0.20) },
        { "18", (1.024, 0.25) },
        { "16", (1.291, 0.30) },
        { "14", (1.628, 0.35) },
        { "12", (2.053, 0.40) },
        { "10", (2.588, 0.45) },
        { "8", (3.264, 0.50) }
    };

    /// <summary>
    /// Get complete cable library from user's library
    /// </summary>
    public static Dictionary<string, Cable> GetCompleteCableLibrary()
    {
        return UserLibraryService.LoadCableLibrary();
    }

    /// <summary>
    /// Get complete heat shrink library from user's library
    /// </summary>
    public static Dictionary<string, HeatShrink> GetCompleteHeatShrinkLibrary()
    {
        return UserLibraryService.LoadHeatShrinkLibrary();
    }

    /// <summary>
    /// Create a multi-core cable with standard DIN colors
    /// Helper method for cable creation dialogs
    /// </summary>
    public static Cable CreateMultiCoreCable(string partNumber, string name, string manufacturer,
        int coreCount, double conductorDiameter, double insulationThickness, string jacketColor, bool shielded)
    {
        return new Cable
        {
            PartNumber = partNumber,
            Name = name,
            Manufacturer = manufacturer,
            Type = CableType.MultiCore,
            HasShield = shielded,
            ShieldType = shielded ? ShieldType.Braid : ShieldType.None,
            ShieldThickness = shielded ? 0.15 : 0,
            JacketColor = jacketColor,
            JacketThickness = Math.Max(0.5, 0.3 + coreCount * 0.02),
            Cores = CreateColoredCores(coreCount, conductorDiameter, insulationThickness)
        };
    }

    /// <summary>
    /// Create colored cores using standard DIN color scheme
    /// </summary>
    public static List<CableCore> CreateColoredCores(int count, double conductorDiameter, double insulationThickness)
    {
        var dinColors = new[] { "Green/Yellow", "Blue", "Brown", "Black", "Gray", "White", "Red", "Orange", "Violet", "Pink" };
        var cores = new List<CableCore>();

        for (int i = 0; i < count; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = conductorDiameter,
                InsulationThickness = insulationThickness,
                InsulationColor = dinColors[i % dinColors.Length],
                Gauge = GetAwgFromDiameter(conductorDiameter),
                ConductorMaterial = "Copper"
            });
        }

        return cores;
    }

    /// <summary>
    /// Get AWG gauge from conductor diameter
    /// </summary>
    public static string GetAwgFromDiameter(double diameter)
    {
        return diameter switch
        {
            <= 0.28 => "30",
            <= 0.35 => "28",
            <= 0.45 => "26",
            <= 0.55 => "24",
            <= 0.70 => "22",
            <= 0.90 => "20",
            <= 1.15 => "18",
            <= 1.45 => "16",
            <= 1.80 => "14",
            <= 2.30 => "12",
            <= 2.90 => "10",
            _ => "8"
        };
    }
}
