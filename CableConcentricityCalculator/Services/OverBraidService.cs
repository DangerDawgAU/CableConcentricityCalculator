using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing over-braid and sleeving selections and specifications.
/// All over-braid data is loaded from user library
/// </summary>
public static class OverBraidService
{
    /// <summary>
    /// Get all available over-braids from user library
    /// </summary>
    public static List<OverBraid> GetAllAvailableBraids()
    {
        // Load from user library
        var library = UserLibraryService.LoadOverBraidLibrary();

        if (library.Count == 0)
        {
            Console.WriteLine("Over-braid library is empty. Use the GUI to add over-braids to your library.");
            return new List<OverBraid>();
        }

        return library.Values.OrderBy(b => b.Manufacturer).ThenBy(b => b.NominalInnerDiameter).ToList();
    }

    /// <summary>
    /// Find the most appropriate over-braid for a given cable diameter
    /// Selects based on the diameter fitting within the min-max range, preferring nominal diameter match
    /// </summary>
    public static OverBraid? SelectAppropriateOverBraid(double cableDiameter)
    {
        if (cableDiameter <= 0)
            return null;

        // Get all available over-braids
        var allBraids = GetAllAvailableBraids();

        // Find braids that can accommodate the cable diameter (within min-max range)
        var suitableBraids = allBraids
            .Where(b => cableDiameter >= b.MinInnerDiameter && cableDiameter <= b.MaxInnerDiameter)
            .ToList();

        if (suitableBraids.Count == 0)
        {
            // No exact fit - find the smallest braid that can expand to fit
            var nextLargest = allBraids
                .Where(b => b.MaxInnerDiameter >= cableDiameter)
                .OrderBy(b => b.MaxInnerDiameter)
                .FirstOrDefault();

            return nextLargest;
        }

        // Prefer braids where the cable diameter is closer to the nominal diameter
        var bestMatch = suitableBraids
            .OrderBy(b => Math.Abs(b.NominalInnerDiameter - cableDiameter))
            .FirstOrDefault();

        return bestMatch;
    }

    /// <summary>
    /// Get over-braid by part number
    /// </summary>
    public static OverBraid? GetOverBraidByPartNumber(string partNumber)
    {
        var allBraids = GetAllAvailableBraids();
        return allBraids.FirstOrDefault(b => b.PartNumber == partNumber);
    }
}
