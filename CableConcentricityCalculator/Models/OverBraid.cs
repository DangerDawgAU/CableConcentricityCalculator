namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents an over-braid applied to the cable assembly
/// </summary>
public class OverBraid
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
    /// Braid type
    /// </summary>
    public BraidType Type { get; set; } = BraidType.RoundBraid;

    /// <summary>
    /// Material of the braid
    /// </summary>
    public string Material { get; set; } = "Tinned Copper";

    /// <summary>
    /// Coverage percentage (typically 80-98%)
    /// </summary>
    public double CoveragePercent { get; set; } = 85;

    /// <summary>
    /// Nominal inner diameter in mm (relaxed state)
    /// </summary>
    public double NominalInnerDiameter { get; set; }

    /// <summary>
    /// Maximum expanded inner diameter in mm
    /// </summary>
    public double MaxInnerDiameter { get; set; }

    /// <summary>
    /// Minimum contracted inner diameter in mm
    /// </summary>
    public double MinInnerDiameter { get; set; }

    /// <summary>
    /// Wall thickness in mm
    /// </summary>
    public double WallThickness { get; set; }

    /// <summary>
    /// Number of carriers/bobbins
    /// </summary>
    public int CarrierCount { get; set; } = 16;

    /// <summary>
    /// Number of ends per carrier
    /// </summary>
    public int EndsPerCarrier { get; set; } = 6;

    /// <summary>
    /// Wire diameter in mm (for wire braids)
    /// </summary>
    public double WireDiameter { get; set; }

    /// <summary>
    /// Color of the braid
    /// </summary>
    public string Color { get; set; } = "Silver";

    /// <summary>
    /// Whether this is an EMI/RFI shielding braid
    /// </summary>
    public bool IsShielding { get; set; } = true;

    /// <summary>
    /// Picks per inch (braiding density)
    /// </summary>
    public double PicksPerInch { get; set; } = 12;

    /// <summary>
    /// Layer number this braid is applied over
    /// </summary>
    public int AppliedOverLayer { get; set; } = -1; // -1 means outermost core

    /// <summary>
    /// Notes for assembly
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Calculate effective diameter addition
    /// </summary>
    public double DiameterAddition => WallThickness * 2;

    /// <summary>
    /// Check if this braid can fit over a given diameter
    /// </summary>
    public bool CanFitOver(double diameter)
    {
        return diameter >= MinInnerDiameter && diameter <= MaxInnerDiameter;
    }

    /// <summary>
    /// Calculate the working diameter of the braid when installed
    /// </summary>
    public double GetWorkingDiameter(double overDiameter)
    {
        return overDiameter + DiameterAddition;
    }

    public override string ToString()
    {
        return $"{PartNumber} ({Material}, {CoveragePercent}% coverage)";
    }
}

/// <summary>
/// Types of braiding
/// </summary>
public enum BraidType
{
    /// <summary>
    /// Standard round tubular braid
    /// </summary>
    RoundBraid,

    /// <summary>
    /// Flat braid
    /// </summary>
    FlatBraid,

    /// <summary>
    /// Expandable sleeving
    /// </summary>
    ExpandableSleeving,

    /// <summary>
    /// Spiral wrap
    /// </summary>
    SpiralWrap,

    /// <summary>
    /// Serve/serving shield
    /// </summary>
    Serving
}

/// <summary>
/// Standard braid materials
/// </summary>
public static class BraidMaterials
{
    public const string TinnedCopper = "Tinned Copper";
    public const string BareCopper = "Bare Copper";
    public const string SilverPlatedCopper = "Silver Plated Copper";
    public const string NickelPlatedCopper = "Nickel Plated Copper";
    public const string Aluminum = "Aluminum";
    public const string StainlessSteel = "Stainless Steel";
    public const string Nylon = "Nylon";
    public const string Polyester = "Polyester (PET)";
    public const string Nomex = "Nomex";
    public const string Fiberglass = "Fiberglass";

    public static readonly string[] Conductive = {
        TinnedCopper, BareCopper, SilverPlatedCopper, NickelPlatedCopper, Aluminum, StainlessSteel
    };

    public static readonly string[] NonConductive = {
        Nylon, Polyester, Nomex, Fiberglass
    };

    public static readonly string[] All = Conductive.Concat(NonConductive).ToArray();
}
