using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing heat shrink tubing selections and specifications
/// </summary>
public static class HeatShrinkService
{
    /// <summary>
    /// Raychem DR25 heat shrink specifications (2:1 shrink ratio, polyolefin)
    /// Format: (supplied_inner_diameter_mm, recovered_inner_diameter_mm) → part_number
    /// </summary>
    private static readonly Dictionary<(double, double), string> RaychemDR25Catalog = new()
    {
        // DR25 series - 2:1 shrink ratio
        { (1.6, 0.8), "DR25-0.8/0.4" },      // 0.8mm recovered ID
        { (2.4, 1.2), "DR25-1.2/0.6" },      // 1.2mm recovered ID
        { (3.2, 1.6), "DR25-1.6/0.8" },      // 1.6mm recovered ID
        { (4.0, 2.0), "DR25-2.0/1.0" },      // 2.0mm recovered ID
        { (4.8, 2.4), "DR25-2.4/1.2" },      // 2.4mm recovered ID
        { (6.4, 3.2), "DR25-3.2/1.6" },      // 3.2mm recovered ID
        { (8.0, 4.0), "DR25-4.0/2.0" },      // 4.0mm recovered ID
        { (9.6, 4.8), "DR25-4.8/2.4" },      // 4.8mm recovered ID
        { (11.2, 5.6), "DR25-5.6/2.8" },     // 5.6mm recovered ID
        { (12.8, 6.4), "DR25-6.4/3.2" },     // 6.4mm recovered ID
        { (16.0, 8.0), "DR25-8.0/4.0" },     // 8.0mm recovered ID
        { (19.2, 9.6), "DR25-9.6/4.8" },     // 9.6mm recovered ID
        { (22.4, 11.2), "DR25-11.2/5.6" },   // 11.2mm recovered ID
        { (25.6, 12.8), "DR25-12.8/6.4" },   // 12.8mm recovered ID
        { (32.0, 16.0), "DR25-16.0/8.0" },   // 16.0mm recovered ID
        { (38.4, 19.2), "DR25-19.2/9.6" },   // 19.2mm recovered ID
        { (44.8, 22.4), "DR25-22.4/11.2" },  // 22.4mm recovered ID
        { (51.2, 25.6), "DR25-25.6/12.8" },  // 25.6mm recovered ID
        { (64.0, 32.0), "DR25-32.0/16.0" },  // 32.0mm recovered ID
    };

    /// <summary>
    /// Get all available heat shrink options - loads from JSON
    /// </summary>
    public static List<HeatShrink> GetAvailableHeatShrinks()
    {
        // Load from JSON library
        var library = LibraryLoader.LoadHeatShrinkLibrary();

        if (library.Count == 0)
        {
            throw new InvalidOperationException(
                "Heat shrink library not found. Please ensure HeatShrinkLibrary.json exists in the Libraries folder.");
        }

        return library.Values.OrderBy(h => h.SuppliedInnerDiameter).ToList();
    }

    /// <summary>
    /// Find the most appropriate DR25 heat shrink for a given outer diameter
    /// Uses 10-20% clearance rule: supplied ID should be 1.1x to 1.2x the assembly OD
    /// </summary>
    public static HeatShrink? SelectAppropriateHeatShrink(double assemblyOuterDiameter)
    {
        if (assemblyOuterDiameter <= 0)
            return null;

        // Target supplied ID with 10-20% clearance for easy sliding
        double targetSuppliedId = assemblyOuterDiameter * 1.15;

        // Get all available shrinks
        var allShrinks = GetAvailableHeatShrinks();

        // Find the best match: smallest supplied ID that's still >= target
        var bestMatch = allShrinks
            .Where(h => h.SuppliedInnerDiameter >= targetSuppliedId)
            .OrderBy(h => h.SuppliedInnerDiameter)
            .FirstOrDefault();

        // If no exact match found, use the largest available (better than nothing)
        if (bestMatch == null)
            bestMatch = allShrinks.LastOrDefault();

        return bestMatch;
    }

    /// <summary>
    /// Get heat shrink by part number
    /// </summary>
    public static HeatShrink? GetHeatShrinkByPartNumber(string partNumber)
    {
        var allShrinks = GetAvailableHeatShrinks();
        return allShrinks.FirstOrDefault(h => h.PartNumber == partNumber);
    }
}
