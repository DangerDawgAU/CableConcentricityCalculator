using System.Text.Json;
using System.Text.Json.Serialization;
using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Service for saving and loading cable assembly configurations
/// </summary>
public class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Save a cable assembly to a JSON file
    /// </summary>
    public static async Task SaveAssemblyAsync(CableAssembly assembly, string filePath)
    {
        var json = JsonSerializer.Serialize(assembly, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Load a cable assembly from a JSON file
    /// </summary>
    public static async Task<CableAssembly> LoadAssemblyAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<CableAssembly>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize assembly");
    }

    /// <summary>
    /// Save a cable assembly synchronously
    /// </summary>
    public static void SaveAssembly(CableAssembly assembly, string filePath)
    {
        var json = JsonSerializer.Serialize(assembly, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load a cable assembly synchronously
    /// </summary>
    public static CableAssembly LoadAssembly(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<CableAssembly>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize assembly");
    }

    /// <summary>
    /// Create a sample cable library (now loads from user library)
    /// </summary>
    public static Dictionary<string, Cable> CreateSampleCableLibrary()
    {
        return CableLibrary.GetCompleteCableLibrary();
    }

    /// <summary>
    /// Get cables filtered by category (deprecated - categories removed in favor of manufacturer filtering)
    /// </summary>
    [Obsolete("Categories are deprecated. Use manufacturer filtering instead.")]
    public static Dictionary<string, Cable> GetCablesByCategory(string category)
    {
        // Return all cables - categories are no longer used
        return CableLibrary.GetCompleteCableLibrary();
    }

    /// <summary>
    /// Get available cable categories (deprecated - returns empty list)
    /// </summary>
    [Obsolete("Categories are deprecated. Filter by manufacturer instead.")]
    public static string[] GetCableCategories()
    {
        return new[] { "All" };
    }

    /// <summary>
    /// Create sample heat shrink library (deprecated - use UserLibraryService)
    /// </summary>
    [Obsolete("Use UserLibraryService.LoadHeatShrinkLibrary() instead")]
    public static Dictionary<string, HeatShrink> CreateSampleHeatShrinkLibrary()
    {
        return CableLibrary.GetCompleteHeatShrinkLibrary();
    }

    /// <summary>
    /// Create sample over-braid library (deprecated - use UserLibraryService)
    /// </summary>
    [Obsolete("Use UserLibraryService.LoadOverBraidLibrary() instead")]
    public static Dictionary<string, OverBraid> CreateSampleOverBraidLibrary()
    {
        return UserLibraryService.LoadOverBraidLibrary();
    }

    /// <summary>
    /// Create a sample assembly for demonstration
    /// NOTE: This creates a basic assembly with generic cables for demonstration purposes.
    /// In production, users should create assemblies using cables from their user library.
    /// </summary>
    public static CableAssembly CreateSampleAssembly()
    {
        var assembly = new CableAssembly
        {
            PartNumber = "CA-001",
            Revision = "A",
            Name = "Sample Concentric Cable Assembly",
            ProjectReference = "DEMO-001",
            DesignedBy = "Cable Designer",
            Length = 1000,
            Notes = "Demonstration cable assembly. Add your own cables via the GUI.",
            TemperatureRating = 125,
            VoltageRating = 300,
            ApplicableStandards = new List<string> { "User-defined assembly" }
        };

        // Create a simple generic cable for demonstration
        var CreateGenericCable = (string color) => new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8],
            PartNumber = $"GENERIC-22AWG-{color}",
            Name = $"Generic 22 AWG Wire - {color}",
            Manufacturer = "Generic",
            Type = CableType.SingleCore,
            JacketColor = color,
            JacketThickness = 0.20,
            HasShield = false,
            ShieldType = ShieldType.None,
            Cores = new List<CableCore>
            {
                new CableCore
                {
                    CoreId = "1",
                    ConductorDiameter = 0.644,
                    InsulationThickness = 0.20,
                    InsulationColor = color,
                    Gauge = "22",
                    ConductorMaterial = "Copper"
                }
            }
        };

        // Layer 0: Center - 1 cable
        var layer0 = new CableLayer
        {
            LayerNumber = 0,
            TwistDirection = TwistDirection.None,
            Cables = new System.Collections.ObjectModel.ObservableCollection<Cable>
            {
                CreateGenericCable("White")
            }
        };
        assembly.Layers.Add(layer0);

        // Layer 1: 6 cables around center
        var layer1 = new CableLayer
        {
            LayerNumber = 1,
            TwistDirection = TwistDirection.RightHand,
            LayLength = 30,
            Cables = new System.Collections.ObjectModel.ObservableCollection<Cable>
            {
                CreateGenericCable("Black"),
                CreateGenericCable("Red"),
                CreateGenericCable("Green"),
                CreateGenericCable("Blue"),
                CreateGenericCable("Yellow"),
                CreateGenericCable("Orange")
            }
        };
        assembly.Layers.Add(layer1);

        // Layer 2: 12 cables
        var layer2 = new CableLayer
        {
            LayerNumber = 2,
            TwistDirection = TwistDirection.LeftHand,
            LayLength = 40,
            Cables = new System.Collections.ObjectModel.ObservableCollection<Cable>()
        };

        string[] layer2Colors = { "White", "Black", "Red", "Green", "Blue", "Yellow",
                                  "Orange", "Brown", "Violet", "Gray", "White", "Black" };
        foreach (var color in layer2Colors)
        {
            layer2.Cables.Add(CreateGenericCable(color));
        }
        assembly.Layers.Add(layer2);

        // Add tape wrap
        layer2.TapeWrap = new TapeWrap
        {
            Material = "PTFE",
            Thickness = 0.05,
            Width = 12.7,
            OverlapPercent = 50,
            Color = "White",
            PartNumber = "PTFE-TAPE-12"
        };

        // Add over-braid
        assembly.OverBraids.Add(new OverBraid
        {
            PartNumber = "TC-1/4",
            Name = "Tinned Copper Braid 1/4\"",
            Manufacturer = "Alpha Wire",
            Type = BraidType.ExpandableSleeving,
            Material = "Tinned Copper",
            CoveragePercent = 85,
            NominalInnerDiameter = 6.4,
            MinInnerDiameter = 3.2,
            MaxInnerDiameter = 12.7,
            WallThickness = 0.5,
            IsShielding = true
        });

        // Add heat shrink
        assembly.HeatShrinks.Add(new HeatShrink
        {
            PartNumber = "DR-25-12",
            Name = "DR-25 12mm Heat Shrink",
            Manufacturer = "Raychem",
            Material = "Polyolefin",
            SuppliedInnerDiameter = 12.0,
            RecoveredInnerDiameter = 6.0,
            RecoveredWallThickness = 0.8,
            ShrinkRatio = "2:1",
            Color = "Black",
            TemperatureRating = 135
        });

        return assembly;
    }

    private static Cable CloneCable(Cable source)
    {
        return new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8],
            PartNumber = source.PartNumber,
            Manufacturer = source.Manufacturer,
            Name = source.Name,
            Type = source.Type,
            JacketThickness = source.JacketThickness,
            JacketColor = source.JacketColor,
            HasShield = source.HasShield,
            ShieldType = source.ShieldType,
            ShieldThickness = source.ShieldThickness,
            ShieldCoverage = source.ShieldCoverage,
            HasDrainWire = source.HasDrainWire,
            DrainWireDiameter = source.DrainWireDiameter,
            SpecifiedOuterDiameter = source.SpecifiedOuterDiameter,
            Cores = source.Cores.Select(c => new CableCore
            {
                CoreId = c.CoreId,
                ConductorDiameter = c.ConductorDiameter,
                InsulationThickness = c.InsulationThickness,
                InsulationColor = c.InsulationColor,
                Gauge = c.Gauge,
                ConductorMaterial = c.ConductorMaterial
            }).ToList()
        };
    }
}
