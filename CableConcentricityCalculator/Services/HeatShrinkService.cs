using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing heat shrink tubing selections and specifications.
/// All heat shrink data is loaded from user library
/// </summary>
public static class HeatShrinkService
{
    /// <summary>
    /// Get all available heat shrink options from user library
    /// </summary>
    public static List<HeatShrink> GetAvailableHeatShrinks()
    {
        // Load from user library
        var library = UserLibraryService.LoadHeatShrinkLibrary();

        if (library.Count == 0)
        {
            Console.WriteLine("Heat shrink library is empty. Use the GUI to add heat shrinks to your library.");
            return new List<HeatShrink>();
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
