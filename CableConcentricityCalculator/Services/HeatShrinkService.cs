using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing heat shrink tubing selections and specifications.
/// All heat shrink data is loaded from HeatShrinkLibrary.json
/// </summary>
public static class HeatShrinkService
{
    /// <summary>
    /// Get all available heat shrink options from JSON library
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
