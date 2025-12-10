using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing over-braid and sleeving selections and specifications
/// </summary>
public static class OverBraidService
{
    /// <summary>
    /// MDPC-X cable sleeving specifications
    /// Format: (nom_inner_diameter_mm, min_inner_diameter_mm, max_inner_diameter_mm, wall_thickness_mm) → size_name
    /// MDPC-X is a premium expandable PET sleeving brand popular for custom cable sleeving
    /// </summary>
    private static readonly Dictionary<(double, double, double, double), string> MDPCXCatalog = new()
    {
        // MDPC-X Small (XS) - for very thin wires
        { (3.0, 2.0, 4.0, 0.25), "MDPC-X Small (XS)" },

        // MDPC-X Small - standard small size
        { (4.0, 3.0, 5.5, 0.30), "MDPC-X Small" },

        // MDPC-X Medium - most common size for PC cables
        { (6.0, 4.5, 8.0, 0.35), "MDPC-X Medium" },

        // MDPC-X Large - for thicker cable bundles
        { (8.0, 6.0, 10.5, 0.40), "MDPC-X Large" },

        // MDPC-X XL - for very thick bundles
        { (10.0, 7.5, 13.0, 0.45), "MDPC-X XL" },

        // MDPC-X XXL - for maximum expansion
        { (13.0, 10.0, 17.0, 0.50), "MDPC-X XXL" },
    };

    /// <summary>
    /// MDPC-X color options (subset of their extensive catalog)
    /// </summary>
    public static readonly string[] MDPCXColors = new[]
    {
        "Anthracite",
        "Black",
        "White",
        "Red",
        "Dark Red",
        "Orange",
        "Yellow",
        "Green",
        "Dark Green",
        "Blue",
        "Sky Blue",
        "Dark Blue",
        "Purple",
        "Violet",
        "Pink",
        "Gray",
        "Silver",
        "Graphite",
        "Beige",
        "Brown",
        "Copper",
        "Gold"
    };

    /// <summary>
    /// Get all available MDPC-X sleeving options in a specific color
    /// </summary>
    public static List<OverBraid> GetAvailableMDPCXSleeving(string color = "Black")
    {
        var sleevings = new List<OverBraid>();

        foreach (var ((nomId, minId, maxId, wallThickness), sizeName) in MDPCXCatalog)
        {
            sleevings.Add(new OverBraid
            {
                PartNumber = $"MDPC-X-{sizeName.Split(' ').Last()}-{color.Replace(" ", "")}",
                Name = $"{sizeName} - {color}",
                Manufacturer = "MDPC-X",
                Type = BraidType.ExpandableSleeving,
                Material = "Polyester (PET)",
                NominalInnerDiameter = nomId,
                MinInnerDiameter = minId,
                MaxInnerDiameter = maxId,
                WallThickness = wallThickness,
                CoveragePercent = 98,  // MDPC-X has very tight weave
                Color = color,
                IsShielding = false,  // PET sleeving is for protection/aesthetics, not EMI shielding
                CarrierCount = 24,
                EndsPerCarrier = 8,
                PicksPerInch = 16  // Tight weave
            });
        }

        return sleevings.OrderBy(s => s.NominalInnerDiameter).ToList();
    }

    /// <summary>
    /// Get all MDPC-X sizes in all available colors
    /// </summary>
    public static List<OverBraid> GetAllMDPCXSleeving()
    {
        var allSleevings = new List<OverBraid>();

        foreach (var color in MDPCXColors)
        {
            allSleevings.AddRange(GetAvailableMDPCXSleeving(color));
        }

        return allSleevings.OrderBy(s => s.NominalInnerDiameter).ThenBy(s => s.Color).ToList();
    }

    /// <summary>
    /// Find the most appropriate MDPC-X sleeving for a given outer diameter
    /// Sleeving should expand 10-30% to fit properly
    /// </summary>
    public static OverBraid? SelectAppropriateMDPCXSleeving(double assemblyOuterDiameter, string color = "Black")
    {
        if (assemblyOuterDiameter <= 0)
            return null;

        // Get all available sleevings in the specified color
        var allSleevings = GetAvailableMDPCXSleeving(color);

        // Find best match: sleeving where assembly OD is between min and nominal diameter
        // This ensures good fit with some expansion but not over-stretched
        var bestMatch = allSleevings
            .Where(s => assemblyOuterDiameter >= s.MinInnerDiameter &&
                       assemblyOuterDiameter <= s.NominalInnerDiameter * 1.15)
            .OrderBy(s => Math.Abs(s.NominalInnerDiameter - assemblyOuterDiameter))
            .FirstOrDefault();

        // If no match, find one that can physically fit
        if (bestMatch == null)
        {
            bestMatch = allSleevings
                .Where(s => s.CanFitOver(assemblyOuterDiameter))
                .OrderBy(s => s.NominalInnerDiameter)
                .FirstOrDefault();
        }

        return bestMatch;
    }

    /// <summary>
    /// Get over-braid by part number
    /// </summary>
    public static OverBraid? GetOverBraidByPartNumber(string partNumber)
    {
        var allSleevings = GetAllMDPCXSleeving();
        return allSleevings.FirstOrDefault(s => s.PartNumber == partNumber);
    }

    /// <summary>
    /// Get standard metallic braids for EMI/RFI shielding
    /// </summary>
    public static List<OverBraid> GetStandardShieldingBraids()
    {
        var braids = new List<OverBraid>();

        // Common tinned copper braid sizes
        var sizes = new[]
        {
            (3.0, 0.15, "3mm"),
            (4.0, 0.18, "4mm"),
            (5.0, 0.20, "5mm"),
            (6.0, 0.22, "6mm"),
            (8.0, 0.25, "8mm"),
            (10.0, 0.28, "10mm"),
            (12.0, 0.30, "12mm"),
            (15.0, 0.35, "15mm"),
            (20.0, 0.40, "20mm"),
            (25.0, 0.45, "25mm")
        };

        foreach (var (nominalId, wallThickness, sizeName) in sizes)
        {
            braids.Add(new OverBraid
            {
                PartNumber = $"BRAID-TC-{sizeName}",
                Name = $"Tinned Copper Braid {sizeName}",
                Manufacturer = "Generic",
                Type = BraidType.RoundBraid,
                Material = BraidMaterials.TinnedCopper,
                NominalInnerDiameter = nominalId,
                MinInnerDiameter = nominalId * 0.85,
                MaxInnerDiameter = nominalId * 1.15,
                WallThickness = wallThickness,
                CoveragePercent = 90,
                Color = "Silver",
                IsShielding = true,
                CarrierCount = 16,
                EndsPerCarrier = 6,
                PicksPerInch = 12
            });
        }

        return braids;
    }

    /// <summary>
    /// Get all available over-braids (MDPC-X + standard shielding braids)
    /// </summary>
    public static List<OverBraid> GetAllAvailableBraids()
    {
        var allBraids = new List<OverBraid>();
        allBraids.AddRange(GetAllMDPCXSleeving());
        allBraids.AddRange(GetStandardShieldingBraids());
        return allBraids.OrderBy(b => b.Manufacturer).ThenBy(b => b.NominalInnerDiameter).ToList();
    }
}
