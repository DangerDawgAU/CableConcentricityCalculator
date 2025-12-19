using System.Text.Json;
using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for managing user's personal cable, heat shrink, and over-braid libraries
/// Libraries are stored in the UserLibrary folder in the current working directory
/// </summary>
public static class UserLibraryService
{
    private static readonly string UserLibraryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "UserLibrary"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Load user's cable library from UserLibrary.json
    /// </summary>
    public static Dictionary<string, Cable> LoadCableLibrary()
    {
        var library = new Dictionary<string, Cable>();
        var filePath = Path.Combine(UserLibraryPath, "UserCableLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"User cable library not found at: {filePath}");
            Console.WriteLine("Creating empty user library. Use the GUI to add cables.");
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
                    if (string.IsNullOrEmpty(cable.CableId))
                    {
                        cable.CableId = Guid.NewGuid().ToString("N")[..8];
                    }
                    library[cable.PartNumber] = cable;
                }
            }

            Console.WriteLine($"Loaded {library.Count} cables from user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user cable library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Save user's cable library to UserLibrary.json
    /// </summary>
    public static void SaveCableLibrary(Dictionary<string, Cable> library)
    {
        var filePath = Path.Combine(UserLibraryPath, "UserCableLibrary.json");

        try
        {
            Directory.CreateDirectory(UserLibraryPath);

            var data = new CableLibraryData
            {
                Cables = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} cables to user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving user cable library: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Add a cable to the user library
    /// </summary>
    public static void AddCable(Cable cable)
    {
        var library = LoadCableLibrary();

        if (string.IsNullOrEmpty(cable.CableId))
        {
            cable.CableId = Guid.NewGuid().ToString("N")[..8];
        }

        library[cable.PartNumber] = cable;
        SaveCableLibrary(library);
    }

    /// <summary>
    /// Remove a cable from the user library
    /// </summary>
    public static void RemoveCable(string partNumber)
    {
        var library = LoadCableLibrary();
        library.Remove(partNumber);
        SaveCableLibrary(library);
    }

    /// <summary>
    /// Update a cable in the user library
    /// </summary>
    public static void UpdateCable(Cable cable)
    {
        AddCable(cable); // Same as add - will overwrite existing
    }

    /// <summary>
    /// Load user's heat shrink library
    /// </summary>
    public static Dictionary<string, HeatShrink> LoadHeatShrinkLibrary()
    {
        var library = new Dictionary<string, HeatShrink>();
        var filePath = Path.Combine(UserLibraryPath, "UserHeatShrinkLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"User heat shrink library not found at: {filePath}");
            Console.WriteLine("Creating empty user library. Use the GUI to add heat shrinks.");
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
                    shrink.Id = Guid.NewGuid().ToString("N")[..8];
                    library[shrink.PartNumber] = shrink;
                }
            }

            Console.WriteLine($"Loaded {library.Count} heat shrinks from user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user heat shrink library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Save user's heat shrink library
    /// </summary>
    public static void SaveHeatShrinkLibrary(Dictionary<string, HeatShrink> library)
    {
        var filePath = Path.Combine(UserLibraryPath, "UserHeatShrinkLibrary.json");

        try
        {
            Directory.CreateDirectory(UserLibraryPath);

            var data = new HeatShrinkLibraryData
            {
                HeatShrinks = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} heat shrinks to user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving user heat shrink library: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Add a heat shrink to the user library
    /// </summary>
    public static void AddHeatShrink(HeatShrink heatShrink)
    {
        var library = LoadHeatShrinkLibrary();

        if (string.IsNullOrEmpty(heatShrink.Id))
        {
            heatShrink.Id = Guid.NewGuid().ToString("N")[..8];
        }

        library[heatShrink.PartNumber] = heatShrink;
        SaveHeatShrinkLibrary(library);
    }

    /// <summary>
    /// Load user's over-braid library
    /// </summary>
    public static Dictionary<string, OverBraid> LoadOverBraidLibrary()
    {
        var library = new Dictionary<string, OverBraid>();
        var filePath = Path.Combine(UserLibraryPath, "UserOverBraidLibrary.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"User over-braid library not found at: {filePath}");
            Console.WriteLine("Creating empty user library. Use the GUI to add over-braids.");
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
                    braid.Id = Guid.NewGuid().ToString("N")[..8];
                    library[braid.PartNumber] = braid;
                }
            }

            Console.WriteLine($"Loaded {library.Count} over-braids from user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user over-braid library: {ex.Message}");
        }

        return library;
    }

    /// <summary>
    /// Save user's over-braid library
    /// </summary>
    public static void SaveOverBraidLibrary(Dictionary<string, OverBraid> library)
    {
        var filePath = Path.Combine(UserLibraryPath, "UserOverBraidLibrary.json");

        try
        {
            Directory.CreateDirectory(UserLibraryPath);

            var data = new OverBraidLibraryData
            {
                OverBraids = library.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved {library.Count} over-braids to user library");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving user over-braid library: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Add an over-braid to the user library
    /// </summary>
    public static void AddOverBraid(OverBraid overBraid)
    {
        var library = LoadOverBraidLibrary();

        if (string.IsNullOrEmpty(overBraid.Id))
        {
            overBraid.Id = Guid.NewGuid().ToString("N")[..8];
        }

        library[overBraid.PartNumber] = overBraid;
        SaveOverBraidLibrary(library);
    }

    /// <summary>
    /// Get the path to the user library folder
    /// </summary>
    public static string GetUserLibraryPath()
    {
        return UserLibraryPath;
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
