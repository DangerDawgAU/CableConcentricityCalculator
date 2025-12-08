namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a single cable (single or multi-core) in the harness
/// </summary>
public class Cable
{
    /// <summary>
    /// Unique identifier for this cable
    /// </summary>
    public string CableId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Part number for ordering/reference
    /// </summary>
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer name
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive name for the cable
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cable type (single-core, multi-core, coaxial, triaxial, etc.)
    /// </summary>
    public CableType Type { get; set; } = CableType.SingleCore;

    /// <summary>
    /// List of cores in this cable (1 for single-core, multiple for multi-core)
    /// </summary>
    public List<CableCore> Cores { get; set; } = new();

    /// <summary>
    /// Overall jacket/insulation thickness in mm (outer jacket)
    /// </summary>
    public double JacketThickness { get; set; }

    /// <summary>
    /// Jacket/outer insulation color
    /// </summary>
    public string JacketColor { get; set; } = "Black";

    /// <summary>
    /// Whether this cable has a shield
    /// </summary>
    public bool HasShield { get; set; }

    /// <summary>
    /// Shield type if shielded
    /// </summary>
    public ShieldType ShieldType { get; set; } = ShieldType.None;

    /// <summary>
    /// Shield thickness in mm (including drain wire space if applicable)
    /// </summary>
    public double ShieldThickness { get; set; }

    /// <summary>
    /// Shield coverage percentage (for braided shields)
    /// </summary>
    public double ShieldCoverage { get; set; } = 85;

    /// <summary>
    /// Whether this cable has a drain wire
    /// </summary>
    public bool HasDrainWire { get; set; }

    /// <summary>
    /// Drain wire diameter in mm
    /// </summary>
    public double DrainWireDiameter { get; set; }

    /// <summary>
    /// Whether this cable is a filler wire (non-functional, for concentricity)
    /// </summary>
    public bool IsFiller { get; set; }

    /// <summary>
    /// Filler material type
    /// </summary>
    public string FillerMaterial { get; set; } = "Nylon";

    /// <summary>
    /// Calculated core bundle diameter (for multi-core cables)
    /// </summary>
    public double CoreBundleDiameter
    {
        get
        {
            if (Cores.Count == 0) return 0;
            if (Cores.Count == 1) return Cores[0].OverallDiameter;

            // Calculate bundle diameter based on core count using concentric packing
            var maxCoreDiameter = Cores.Max(c => c.OverallDiameter);
            return CalculateBundleDiameter(Cores.Count, maxCoreDiameter);
        }
    }

    /// <summary>
    /// Overall outer diameter of this cable in mm
    /// </summary>
    public double OuterDiameter
    {
        get
        {
            double diameter = CoreBundleDiameter;

            if (HasShield)
            {
                diameter += 2 * ShieldThickness;
            }

            diameter += 2 * JacketThickness;

            return diameter;
        }
    }

    /// <summary>
    /// Cross-sectional area of the cable in mm²
    /// </summary>
    public double CrossSectionalArea => Math.PI * Math.Pow(OuterDiameter / 2, 2);

    /// <summary>
    /// Total conductor cross-sectional area in mm²
    /// </summary>
    public double TotalConductorArea => Cores.Sum(c => c.ConductorArea);

    /// <summary>
    /// Calculate bundle diameter for multiple cores
    /// </summary>
    private static double CalculateBundleDiameter(int coreCount, double coreDiameter)
    {
        // Standard formulas for wire bundle diameters
        return coreCount switch
        {
            1 => coreDiameter,
            2 => 2 * coreDiameter,
            3 => 2.155 * coreDiameter,
            4 => 2.414 * coreDiameter,
            5 => 2.701 * coreDiameter,
            6 => 3 * coreDiameter,
            7 => 3 * coreDiameter,
            _ => CalculateGeneralBundleDiameter(coreCount, coreDiameter)
        };
    }

    /// <summary>
    /// General formula for larger core counts
    /// </summary>
    private static double CalculateGeneralBundleDiameter(int coreCount, double coreDiameter)
    {
        // For larger counts, use approximate formula based on packing efficiency
        // Assumes ~78.5% packing efficiency for random packing
        double totalArea = coreCount * Math.PI * Math.Pow(coreDiameter / 2, 2);
        double bundleArea = totalArea / 0.785;
        return 2 * Math.Sqrt(bundleArea / Math.PI);
    }

    public override string ToString()
    {
        string typeStr = Type == CableType.SingleCore ? "Single" : $"{Cores.Count}-Core";
        return $"{PartNumber} ({typeStr}, {OuterDiameter:F2}mm OD)";
    }
}

/// <summary>
/// Types of cables supported
/// </summary>
public enum CableType
{
    SingleCore,
    MultiCore,
    Coaxial,
    Triaxial,
    TwistedPair,
    TwistedTriple,
    Ribbon,
    Filler
}

/// <summary>
/// Types of shielding
/// </summary>
public enum ShieldType
{
    None,
    Braid,
    Foil,
    FoilAndBraid,
    Spiral,
    ServingShield
}
