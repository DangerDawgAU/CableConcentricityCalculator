namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents heat shrink tubing applied to the cable assembly
/// </summary>
public class HeatShrink
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Part number for ordering
    /// </summary>
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer name
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Product name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Material type (Polyolefin, PTFE, Viton, etc.)
    /// </summary>
    public string Material { get; set; } = "Polyolefin";

    /// <summary>
    /// Supplied (expanded) inner diameter in mm
    /// </summary>
    public double SuppliedInnerDiameter { get; set; }

    /// <summary>
    /// Recovered (shrunk) inner diameter in mm
    /// </summary>
    public double RecoveredInnerDiameter { get; set; }

    /// <summary>
    /// Shrink ratio (e.g., 2:1, 3:1, 4:1)
    /// </summary>
    public string ShrinkRatio { get; set; } = "2:1";

    /// <summary>
    /// Wall thickness after recovery in mm
    /// </summary>
    public double RecoveredWallThickness { get; set; }

    /// <summary>
    /// Color of the heat shrink
    /// </summary>
    public string Color { get; set; } = "Black";

    /// <summary>
    /// Whether this heat shrink has adhesive lining
    /// </summary>
    public bool HasAdhesiveLining { get; set; }

    /// <summary>
    /// Adhesive thickness in mm (if applicable)
    /// </summary>
    public double AdhesiveThickness { get; set; }

    /// <summary>
    /// Temperature rating in Celsius
    /// </summary>
    public int TemperatureRating { get; set; } = 125;

    /// <summary>
    /// Recovery temperature in Celsius
    /// </summary>
    public int RecoveryTemperature { get; set; } = 120;

    /// <summary>
    /// Length to cut in mm
    /// </summary>
    public double CutLength { get; set; }

    /// <summary>
    /// Position along cable assembly (for multiple heat shrink sections)
    /// </summary>
    public double Position { get; set; }

    /// <summary>
    /// Layer number this heat shrink is applied over (for nested heat shrink)
    /// </summary>
    public int AppliedOverLayer { get; set; } = -1; // -1 means outermost

    /// <summary>
    /// Notes for assembly
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Calculate total wall addition to diameter
    /// </summary>
    public double TotalWallAddition =>
        HasAdhesiveLining
            ? (RecoveredWallThickness + AdhesiveThickness) * 2
            : RecoveredWallThickness * 2;

    /// <summary>
    /// Checks if this heat shrink will fit over a given diameter
    /// </summary>
    public bool WillFitOver(double diameter)
    {
        return diameter < SuppliedInnerDiameter && diameter >= RecoveredInnerDiameter;
    }

    /// <summary>
    /// Calculate the minimum cable diameter this heat shrink should be used on
    /// </summary>
    public double MinCableDiameter => RecoveredInnerDiameter;

    /// <summary>
    /// Calculate the maximum cable diameter this heat shrink can fit over
    /// </summary>
    public double MaxCableDiameter => SuppliedInnerDiameter * 0.95; // 5% margin

    public override string ToString()
    {
        return $"{PartNumber} ({Material}, {ShrinkRatio}, {Color})";
    }
}

/// <summary>
/// Standard heat shrink materials
/// </summary>
public static class HeatShrinkMaterials
{
    public const string Polyolefin = "Polyolefin";
    public const string PTFE = "PTFE";
    public const string FEP = "FEP";
    public const string Kynar = "Kynar (PVDF)";
    public const string Viton = "Viton";
    public const string Neoprene = "Neoprene";
    public const string PVC = "PVC";
    public const string Silicone = "Silicone";

    public static readonly string[] All = {
        Polyolefin, PTFE, FEP, Kynar, Viton, Neoprene, PVC, Silicone
    };
}
