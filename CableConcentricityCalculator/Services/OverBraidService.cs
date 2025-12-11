using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing over-braid and sleeving selections and specifications.
/// All over-braid data is loaded from OverBraidLibrary.json
/// </summary>
public static class OverBraidService
{
    /// <summary>
    /// Get all available over-braids from JSON library
    /// </summary>
    public static List<OverBraid> GetAllAvailableBraids()
    {
        // Load from JSON library
        var library = LibraryLoader.LoadOverBraidLibrary();

        if (library.Count == 0)
        {
            throw new InvalidOperationException(
                "Over-braid library not found. Please ensure OverBraidLibrary.json exists in the Libraries folder.");
        }

        return library.Values.OrderBy(b => b.Manufacturer).ThenBy(b => b.NominalInnerDiameter).ToList();
    }
}
