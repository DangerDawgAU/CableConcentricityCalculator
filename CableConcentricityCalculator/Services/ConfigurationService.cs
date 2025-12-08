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
    /// Create a sample cable library
    /// </summary>
    public static Dictionary<string, Cable> CreateSampleCableLibrary()
    {
        var library = new Dictionary<string, Cable>();

        // Single-core wires (MIL-W-22759 style)
        var gauges = new[]
        {
            ("26", 0.405, 0.15),
            ("24", 0.511, 0.15),
            ("22", 0.644, 0.20),
            ("20", 0.812, 0.20),
            ("18", 1.024, 0.25),
            ("16", 1.291, 0.30)
        };

        string[] colors = { "White", "Black", "Red", "Green", "Blue", "Yellow", "Orange", "Brown", "Violet", "Gray" };

        foreach (var (gauge, conductorDia, insulationThick) in gauges)
        {
            foreach (var color in colors)
            {
                var cable = new Cable
                {
                    PartNumber = $"M22759/16-{gauge}-{color[0]}",
                    Name = $"{gauge} AWG {color} Wire",
                    Manufacturer = "Raychem",
                    Type = CableType.SingleCore,
                    JacketColor = color,
                    JacketThickness = insulationThick,
                    Cores = new List<CableCore>
                    {
                        new()
                        {
                            CoreId = "1",
                            ConductorDiameter = conductorDia,
                            InsulationThickness = insulationThick,
                            InsulationColor = color,
                            Gauge = gauge,
                            ConductorMaterial = "Silver Plated Copper"
                        }
                    }
                };
                library[$"M22759-{gauge}-{color}"] = cable;
            }
        }

        // Shielded twisted pairs
        var stpCable = new Cable
        {
            PartNumber = "STP-22-2",
            Name = "22 AWG Shielded Twisted Pair",
            Manufacturer = "Alpha Wire",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.Braid,
            ShieldThickness = 0.15,
            ShieldCoverage = 85,
            JacketThickness = 0.30,
            JacketColor = "Gray",
            Cores = new List<CableCore>
            {
                new()
                {
                    CoreId = "1",
                    ConductorDiameter = 0.644,
                    InsulationThickness = 0.20,
                    InsulationColor = "White",
                    Gauge = "22"
                },
                new()
                {
                    CoreId = "2",
                    ConductorDiameter = 0.644,
                    InsulationThickness = 0.20,
                    InsulationColor = "Blue",
                    Gauge = "22"
                }
            }
        };
        library["STP-22-2"] = stpCable;

        // Coaxial cable
        var coaxCable = new Cable
        {
            PartNumber = "RG-178",
            Name = "RG-178 Coaxial Cable",
            Manufacturer = "Times Microwave",
            Type = CableType.Coaxial,
            HasShield = true,
            ShieldType = ShieldType.Braid,
            ShieldThickness = 0.15,
            ShieldCoverage = 90,
            JacketThickness = 0.25,
            JacketColor = "Brown",
            Cores = new List<CableCore>
            {
                new()
                {
                    CoreId = "Center",
                    ConductorDiameter = 0.305,
                    InsulationThickness = 0.48, // PTFE dielectric
                    InsulationColor = "White",
                    Gauge = "30",
                    ConductorMaterial = "Silver Plated Copper"
                }
            }
        };
        library["RG-178"] = coaxCable;

        // Multi-conductor cable
        var multiConductor = new Cable
        {
            PartNumber = "MC-20-4",
            Name = "4-Conductor 20 AWG Cable",
            Manufacturer = "Belden",
            Type = CableType.MultiCore,
            HasShield = true,
            ShieldType = ShieldType.Foil,
            ShieldThickness = 0.10,
            HasDrainWire = true,
            DrainWireDiameter = 0.51,
            JacketThickness = 0.40,
            JacketColor = "Black",
            Cores = new List<CableCore>
            {
                new() { CoreId = "1", ConductorDiameter = 0.812, InsulationThickness = 0.20, InsulationColor = "Black", Gauge = "20" },
                new() { CoreId = "2", ConductorDiameter = 0.812, InsulationThickness = 0.20, InsulationColor = "Red", Gauge = "20" },
                new() { CoreId = "3", ConductorDiameter = 0.812, InsulationThickness = 0.20, InsulationColor = "White", Gauge = "20" },
                new() { CoreId = "4", ConductorDiameter = 0.812, InsulationThickness = 0.20, InsulationColor = "Green", Gauge = "20" }
            }
        };
        library["MC-20-4"] = multiConductor;

        return library;
    }

    /// <summary>
    /// Create sample heat shrink library
    /// </summary>
    public static Dictionary<string, HeatShrink> CreateSampleHeatShrinkLibrary()
    {
        var library = new Dictionary<string, HeatShrink>();

        // Raychem DR-25 series
        var sizes = new[]
        {
            (3.0, 1.5, 0.5),
            (5.0, 2.5, 0.6),
            (8.0, 4.0, 0.7),
            (12.0, 6.0, 0.8),
            (18.0, 9.0, 1.0),
            (25.0, 12.5, 1.2)
        };

        foreach (var (supplied, recovered, wall) in sizes)
        {
            var hs = new HeatShrink
            {
                PartNumber = $"DR-25-{supplied:F0}",
                Name = $"DR-25 {supplied:F0}mm Heat Shrink",
                Manufacturer = "Raychem",
                Material = "Polyolefin",
                SuppliedInnerDiameter = supplied,
                RecoveredInnerDiameter = recovered,
                RecoveredWallThickness = wall,
                ShrinkRatio = "2:1",
                Color = "Black",
                TemperatureRating = 135
            };
            library[$"DR-25-{supplied:F0}"] = hs;
        }

        // Add some PTFE heat shrink
        var ptfeSizes = new[]
        {
            (4.0, 2.0, 0.3),
            (6.0, 3.0, 0.4),
            (10.0, 5.0, 0.5)
        };

        foreach (var (supplied, recovered, wall) in ptfeSizes)
        {
            var hs = new HeatShrink
            {
                PartNumber = $"PTFE-{supplied:F0}",
                Name = $"PTFE {supplied:F0}mm Heat Shrink",
                Manufacturer = "Zeus",
                Material = "PTFE",
                SuppliedInnerDiameter = supplied,
                RecoveredInnerDiameter = recovered,
                RecoveredWallThickness = wall,
                ShrinkRatio = "2:1",
                Color = "Clear",
                TemperatureRating = 260,
                RecoveryTemperature = 340
            };
            library[$"PTFE-{supplied:F0}"] = hs;
        }

        return library;
    }

    /// <summary>
    /// Create sample over-braid library
    /// </summary>
    public static Dictionary<string, OverBraid> CreateSampleOverBraidLibrary()
    {
        var library = new Dictionary<string, OverBraid>();

        // Expandable braided sleeving sizes
        var sizes = new[]
        {
            ("1/8", 3.2, 1.6, 6.4),
            ("1/4", 6.4, 3.2, 12.7),
            ("3/8", 9.5, 4.8, 19.0),
            ("1/2", 12.7, 6.4, 25.4),
            ("3/4", 19.0, 9.5, 38.0),
            ("1", 25.4, 12.7, 50.8)
        };

        // Tinned copper braids
        foreach (var (name, nominal, min, max) in sizes)
        {
            var braid = new OverBraid
            {
                PartNumber = $"TC-{name}",
                Name = $"Tinned Copper Braid {name}\"",
                Manufacturer = "Alpha Wire",
                Type = BraidType.ExpandableSleeving,
                Material = "Tinned Copper",
                CoveragePercent = 85,
                NominalInnerDiameter = nominal,
                MinInnerDiameter = min,
                MaxInnerDiameter = max,
                WallThickness = 0.5,
                IsShielding = true
            };
            library[$"TC-{name}"] = braid;
        }

        // PET expandable sleeving
        foreach (var (name, nominal, min, max) in sizes)
        {
            var braid = new OverBraid
            {
                PartNumber = $"PET-{name}",
                Name = $"PET Expandable Sleeving {name}\"",
                Manufacturer = "Techflex",
                Type = BraidType.ExpandableSleeving,
                Material = "Polyester (PET)",
                CoveragePercent = 90,
                NominalInnerDiameter = nominal,
                MinInnerDiameter = min,
                MaxInnerDiameter = max,
                WallThickness = 0.3,
                IsShielding = false,
                Color = "Black"
            };
            library[$"PET-{name}"] = braid;
        }

        return library;
    }

    /// <summary>
    /// Create a sample assembly for demonstration
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
            Notes = "Demonstration cable assembly showing concentric layup",
            TemperatureRating = 125,
            VoltageRating = 300,
            ApplicableStandards = new List<string> { "MIL-DTL-27500", "SAE AS22759" }
        };

        // Create cables
        var cableLibrary = CreateSampleCableLibrary();

        // Layer 0: Center - 1 cable
        var layer0 = new CableLayer
        {
            LayerNumber = 0,
            TwistDirection = TwistDirection.None,
            Cables = new List<Cable>
            {
                CloneCable(cableLibrary["M22759-22-White"])
            }
        };
        assembly.Layers.Add(layer0);

        // Layer 1: 6 cables around center
        var layer1 = new CableLayer
        {
            LayerNumber = 1,
            TwistDirection = TwistDirection.RightHand,
            LayLength = 30,
            Cables = new List<Cable>
            {
                CloneCable(cableLibrary["M22759-22-Black"]),
                CloneCable(cableLibrary["M22759-22-Red"]),
                CloneCable(cableLibrary["M22759-22-Green"]),
                CloneCable(cableLibrary["M22759-22-Blue"]),
                CloneCable(cableLibrary["M22759-22-Yellow"]),
                CloneCable(cableLibrary["M22759-22-Orange"])
            }
        };
        assembly.Layers.Add(layer1);

        // Layer 2: 12 cables
        var layer2 = new CableLayer
        {
            LayerNumber = 2,
            TwistDirection = TwistDirection.LeftHand,
            LayLength = 40,
            Cables = new List<Cable>()
        };

        string[] layer2Colors = { "White", "Black", "Red", "Green", "Blue", "Yellow",
                                  "Orange", "Brown", "Violet", "Gray", "White", "Black" };
        foreach (var color in layer2Colors)
        {
            layer2.Cables.Add(CloneCable(cableLibrary[$"M22759-22-{color}"]));
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
