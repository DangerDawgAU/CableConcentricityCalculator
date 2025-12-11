using System.Text.Json;
using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for loading cable, heat shrink, and over-braid libraries from JSON files
/// </summary>
public static class LibraryLoader
{
    private static readonly string LibrariesPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Libraries"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Load cable library from JSON file
    /// </summary>
    public static Dictionary<string, Cable> LoadCableLibrary()
    {
        var library = new Dictionary<string, Cable>();
        var filePath = Path.Combine(LibrariesPath, "CableLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Cable library file not found: {filePath}");
            return library;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<CableLibraryData>(json, JsonOptions);

            if (data?.Cables != null)
            {
                foreach (var cable in data.Cables)
                {
                    // Generate unique ID for each cable
                    cable.CableId = Guid.NewGuid().ToString("N")[..8];
                    library[cable.PartNumber] = cable;
                }
            }

            Console.WriteLine($"Loaded {library.Count} cables from JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cable library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Load heat shrink library from JSON file
    /// </summary>
    public static Dictionary<string, HeatShrink> LoadHeatShrinkLibrary()
    {
        var library = new Dictionary<string, HeatShrink>();
        var filePath = Path.Combine(LibrariesPath, "HeatShrinkLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Heat shrink library file not found: {filePath}");
            return library;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<HeatShrinkLibraryData>(json, JsonOptions);

            if (data?.HeatShrinks != null)
            {
                foreach (var shrink in data.HeatShrinks)
                {
                    // Generate unique ID for each heat shrink
                    shrink.Id = Guid.NewGuid().ToString("N")[..8];
                    library[shrink.PartNumber] = shrink;
                }
            }

            Console.WriteLine($"Loaded {library.Count} heat shrinks from JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading heat shrink library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Load over-braid library from JSON file
    /// </summary>
    public static Dictionary<string, OverBraid> LoadOverBraidLibrary()
    {
        var library = new Dictionary<string, OverBraid>();
        var filePath = Path.Combine(LibrariesPath, "OverBraidLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Over-braid library file not found: {filePath}");
            return library;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<OverBraidLibraryData>(json, JsonOptions);

            if (data?.OverBraids != null)
            {
                foreach (var braid in data.OverBraids)
                {
                    // Generate unique ID for each over-braid
                    braid.Id = Guid.NewGuid().ToString("N")[..8];
                    library[braid.PartNumber] = braid;
                }
            }

            Console.WriteLine($"Loaded {library.Count} over-braids from JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading over-braid library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Save cable library to JSON file
    /// </summary>
    public static void SaveCableLibrary(Dictionary<string, Cable> library)
    {
        var filePath = Path.Combine(LibrariesPath, "CableLibrary.json");

        try
        {
            Directory.CreateDirectory(LibrariesPath);

            var data = new CableLibraryData
            {
                Cables = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} cables to JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cable library: {ex.Message}");
        }
    }

    /// <summary>
    /// Save heat shrink library to JSON file
    /// </summary>
    public static void SaveHeatShrinkLibrary(Dictionary<string, HeatShrink> library)
    {
        var filePath = Path.Combine(LibrariesPath, "HeatShrinkLibrary.json");

        try
        {
            Directory.CreateDirectory(LibrariesPath);

            var data = new HeatShrinkLibraryData
            {
                HeatShrinks = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} heat shrinks to JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving heat shrink library: {ex.Message}");
        }
    }

    /// <summary>
    /// Save over-braid library to JSON file
    /// </summary>
    public static void SaveOverBraidLibrary(Dictionary<string, OverBraid> library)
    {
        var filePath = Path.Combine(LibrariesPath, "OverBraidLibrary.json");

        try
        {
            Directory.CreateDirectory(LibrariesPath);

            var data = new OverBraidLibraryData
            {
                OverBraids = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} over-braids to JSON library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving over-braid library: {ex.Message}");
        }
    }

    // Helper classes for JSON serialization
    private class CableLibraryData
    {
        public List<Cable> Cables { get; set; } = new();
    }

    private class HeatShrinkLibraryData
    {
        public List<HeatShrink> HeatShrinks { get; set; } = new();
    }

    private class OverBraidLibraryData
    {
        public List<OverBraid> OverBraids { get; set; } = new();
    }
}
